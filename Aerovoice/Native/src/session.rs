use crate::{
    INPUT_DEVICE, OUTPUT_DEVICE,
    crypto::Cryptor,
    rtp::{Encrypted, RtpPacket},
    snowflake::Snowflake,
};
use audiopus::{
    Application, Channels, SampleRate,
    coder::{Decoder, Encoder},
};
use byteorder::{BigEndian, WriteBytesExt};

use cpal::{
    InputCallbackInfo, OutputCallbackInfo, Stream, StreamConfig,
    traits::{DeviceTrait, StreamTrait as _},
};
use rtrb::{Consumer, Producer, RingBuffer};
use std::{
    collections::{HashMap, hash_map::Entry},
    net::UdpSocket,
    sync::mpsc::{self, Receiver, Sender},
    thread::{self, JoinHandle},
    time::Instant,
};

struct RecorderState {
    packet_tx: Sender<RtpPacket<Encrypted>>,
    encoder: Encoder,
    ssrc: u32,
    cryptor: Box<dyn Cryptor>,
    sequence: u16,
    instant: Instant,
    secret: Vec<u8>,
    last_silences: [bool; 24],
}

#[derive(Debug)]
#[repr(C)]
pub struct IPInfo {
    ip: [u8; 64],
    port: u16,
}

macro_rules! fallible {
    ($fallible:expr, $return:expr) => {
        match $fallible {
            Ok(val) => val,
            Err(e) => {
                println!("Error: {}", e);
                return $return;
            }
        }
    };

    ($fallible:expr) => {
        match $fallible {
            Ok(val) => val,
            Err(e) => {
                println!("Error: {}", e);
                continue;
            }
        }
    };
}

macro_rules! nullable {
    ($nullable:expr, $return:expr) => {
        match $nullable {
            Some(val) => val,
            None => {
                println!("Error: None value encountered");
                return $return;
            }
        }
    };

    ($nullable:expr) => {
        match $nullable {
            Some(val) => val,
            None => {
                println!("Error: None value encountered");
                continue;
            }
        }
    };
}

struct AudioUser {
    pcm_producer: Producer<i16>,
    stream: Stream,
    decoder: Decoder,
    speaking: bool,
    was_speaking: bool,
    ssrc: u32,
}

struct ThreadInitialiser {
    cryptor: Box<dyn Cryptor>,
    secret: Vec<u8>,
    socket: UdpSocket,
}

#[repr(C)]
pub struct VoiceSession {
    channel: Snowflake,
    secret: Option<Vec<u8>>,
    ip: String,
    port: u16,
    discovered_ip: Option<IPInfo>,
    socket: Option<UdpSocket>,
    ssrc: u32,
    recv_cryptor: Option<Box<dyn Cryptor>>,
    send_cryptor: Option<Box<dyn Cryptor>>,
    poll_thread: JoinHandle<()>,
    poll_thread_initialiser: Sender<ThreadInitialiser>,
    poll_thread_killer: Sender<()>,
    on_speaking: extern "C" fn(u32, bool),
    packet_tx: Option<Sender<RtpPacket<Encrypted>>>,
    recording_stream: Option<Stream>,
}

impl VoiceSession {
    pub fn new(
        ssrc: u32,
        channel: Snowflake,
        ip: String,
        port: u16,
        on_speaking: extern "C" fn(u32, bool),
    ) -> Self {
        println!("connecting to voice session: {}:{}", ip, port);
        let socket = UdpSocket::bind("0.0.0.0:0").expect("failed to bind UDP socket");
        socket
            .set_nonblocking(true)
            .expect("failed to set non-blocking mode");

        socket
            .connect(format!("{}:{}", ip, port))
            .expect("failed to connect to UDP socket");

        let (init_tx, init_rx) = mpsc::channel();
        let (packet_tx, packet_rx) = mpsc::channel();
        let (killer_tx, killer_rx) = mpsc::channel();

        let poll_thread = thread::spawn(move || {
            // wait for the initialiser
            let initializer: ThreadInitialiser =
                init_rx.recv().expect("failed to receive initialiser");
            let cryptor = initializer.cryptor;
            let secret = initializer.secret;
            let socket = initializer.socket;
            let users = HashMap::new();

            Self::poll_thread(
                socket,
                cryptor,
                secret,
                users,
                packet_rx,
                killer_rx,
                on_speaking,
            );
        });

        Self {
            channel,
            secret: None,
            ip,
            port,
            discovered_ip: None,
            socket: Some(socket),
            ssrc,
            recv_cryptor: None,
            send_cryptor: None,
            poll_thread,
            poll_thread_initialiser: init_tx,
            poll_thread_killer: killer_tx,
            on_speaking,
            packet_tx: Some(packet_tx),
            recording_stream: None,
        }
    }

    fn record_audio(data: &[i16], _: &InputCallbackInfo, recorder_state: &mut RecorderState) {
        recorder_state.sequence = recorder_state.sequence.wrapping_add(1);
        let mut packet = vec![0; 2048];

        let length = match recorder_state.encoder.encode(data, &mut packet) {
            Ok(length) => length,
            Err(e) => {
                println!("Error encoding audio: {}", e);
                return;
            }
        };

        let packet = &packet[..length];

        const CLOCK_RATE: u32 = 48000;
        let elapsed_seconds = recorder_state.instant.elapsed().as_secs_f64();
        let timestamp = (elapsed_seconds * CLOCK_RATE as f64) as u32;
        let is_silent = !is_not_silent(data);

        recorder_state.last_silences.rotate_right(1);
        recorder_state.last_silences[0] = is_silent;

        let is_silent = recorder_state.last_silences.iter().all(|&x| x);

        let packet = match RtpPacket::builder()
            .ssrc(recorder_state.ssrc)
            .sequence(recorder_state.sequence)
            .timestamp(timestamp)
            .payload(packet.to_vec())
            .silence(is_silent)
            .build()
        {
            Ok(packet) => packet,
            Err(e) => {
                println!("Error building RTP packet: {}", e);
                return;
            }
        };

        let Ok(packet) = packet.encrypt(
            recorder_state.cryptor.as_mut(),
            recorder_state.secret.as_slice(),
        ) else {
            println!("failed to encrypt packet");
            return;
        };

        if let Err(e) = recorder_state.packet_tx.send(packet) {
            println!("Error sending packet: {}", e);
        }
    }

    pub fn init_poll_thread(&mut self) {
        let socket = self.socket.take().expect("socket is not set");
        let recv_cryptor = self.recv_cryptor.take().expect("cryptor is not set");
        let send_cryptor = self.send_cryptor.take().expect("cryptor is not set");
        let secret = self.secret.take().expect("secret is not set");
        let packet_tx = self.packet_tx.take().expect("packet tx is not set");
        let ssrc = self.ssrc;

        let thread_initialiser = ThreadInitialiser {
            cryptor: recv_cryptor,
            secret: secret.clone(),
            socket,
        };

        self.poll_thread_initialiser
            .send(thread_initialiser)
            .expect("failed to send initialiser to poll thread");

        let config = StreamConfig {
            channels: 2,
            sample_rate: cpal::SampleRate(48000),
            buffer_size: cpal::BufferSize::Fixed(960),
        };

        println!(
            "creating input stream with {}",
            INPUT_DEVICE.name().unwrap_or_default()
        );

        let mut recorder_state = RecorderState {
            packet_tx,
            encoder: Encoder::new(SampleRate::Hz48000, Channels::Stereo, Application::Voip)
                .expect("failed to create encoder"),
            ssrc,
            cryptor: send_cryptor,
            sequence: 0,
            instant: Instant::now(),
            secret,
            last_silences: [true; 24],
        };

        let input = match INPUT_DEVICE.build_input_stream(
            &config,
            move |data, info| {
                Self::record_audio(data, info, &mut recorder_state);
            },
            |e| println!("An error occured in the recorder: {}", e),
            None,
        ) {
            Ok(stream) => stream,
            Err(e) => {
                println!("Error creating input stream: {}", e);
                return;
            }
        };

        if let Err(e) = input.play() {
            println!("Error playing input stream: {}", e)
        }

        self.recording_stream = Some(input);
    }

    fn poll_thread(
        socket: UdpSocket,
        mut cryptor: Box<dyn Cryptor>,
        secret: Vec<u8>,
        mut users: HashMap<u32, AudioUser>,
        packet_rx: Receiver<RtpPacket<Encrypted>>,
        killer_rx: Receiver<()>,
        on_speaking: extern "C" fn(u32, bool),
    ) {
        let mut last_silent = true;
        let mut faux_packets = vec![];

        loop {
            if killer_rx.try_recv().is_ok() {
                println!("killing poll thread");
                break;
            }

            while let Ok(packet) = packet_rx.try_recv() {
                let is_silent = packet.is_silent().unwrap_or(true);
                if last_silent != is_silent {
                    (on_speaking)(packet.ssrc, !is_silent);
                }

                if !is_silent {
                    if let Err(e) = socket.send(packet.raw()) {
                        println!("Error sending packet: {}", e);
                    }
                }

                last_silent = is_silent;

                faux_packets.push(packet);
            }

            let mut buf = [0; 1024];

            let len = match socket.recv(&mut buf) {
                Ok(len) => len,
                Err(e) => {
                    if e.kind() != std::io::ErrorKind::WouldBlock {
                        println!("Error receiving data: {}", e);
                    }

                    continue;
                }
            };

            let packet = &buf[..len];

            if packet[0] == 129 && packet[1] == 201 {
                continue;
            }

            // let Ok(packet) = RtpPacket::parse(packet) else {
            //     println!("failed to parse packet");
            //     continue;
            // };

            let Some(packet) = faux_packets.pop() else {
                continue;
            };

            if let Entry::Vacant(e) = users.entry(packet.ssrc) {
                let decoder = fallible!(Decoder::new(SampleRate::Hz48000, Channels::Stereo));
                let config = StreamConfig {
                    channels: 2,
                    sample_rate: cpal::SampleRate(48000),
                    buffer_size: cpal::BufferSize::Default,
                };

                // allow up to 1024 packets in the buffer
                let (producer, mut consumer) = RingBuffer::new(960 * 2 * 2 * 1024);

                let stream = OUTPUT_DEVICE.build_output_stream(
                    &config,
                    move |data, info| {
                        Self::process_audio(&mut consumer, data, info);
                    },
                    move |err| {
                        eprintln!("Error: {}", err);
                    },
                    None,
                );

                let stream = fallible!(stream);
                let user = AudioUser {
                    stream,
                    pcm_producer: producer,
                    decoder,
                    speaking: false,
                    was_speaking: false,
                    ssrc: packet.ssrc,
                };

                fallible!(user.stream.play());

                println!("a new user was detected with ssrc {}", packet.ssrc);
                e.insert(user);
            }

            let user = nullable!(users.get_mut(&packet.ssrc));
            let mut output = vec![0; 960 * 2];

            if let Ok(packet) = packet.decrypt(cryptor.as_mut(), secret.as_slice()) {
                if user
                    .decoder
                    .decode(Some(packet.payload()), &mut output, false)
                    .is_ok()
                {
                    for byte in &output {
                        if let Err(e) = user.pcm_producer.push(*byte) {
                            println!("Error pushing to PCM buffer: {}", e);
                        }
                    }

                    user.speaking = true;
                } else {
                    user.speaking = false;
                }
            } else {
                println!("failed to decrypt packet");
                user.speaking = false;
            }

            if user.was_speaking != user.speaking {
                (on_speaking)(user.ssrc, user.speaking);
            }

            user.was_speaking = user.speaking;
        }
    }

    pub fn set_cryptor(&mut self, send_cryptor: Box<dyn Cryptor>, recv_cryptor: Box<dyn Cryptor>) {
        self.recv_cryptor = Some(recv_cryptor);
        self.send_cryptor = Some(send_cryptor);
    }

    pub fn process_audio(pcm_buffer: &mut Consumer<i16>, data: &mut [i16], _: &OutputCallbackInfo) {
        for item in data.iter_mut() {
            match pcm_buffer.pop() {
                Ok(sample) => *item = sample,
                Err(_) => *item = 0,
            };
        }
    }

    pub fn discover_ip(&mut self) -> &IPInfo {
        if let Some(ref ip_info) = self.discovered_ip {
            return ip_info;
        }

        println!("attempting to discover IP...");
        let Some(ref socket) = self.socket else {
            panic!("socket is not set");
        };

        let mut ip_discovery = Vec::with_capacity(2 + 2 + 4 + 64 + 2);
        ip_discovery.write_u16::<BigEndian>(0x1).unwrap();
        ip_discovery.write_u16::<BigEndian>(70).unwrap();
        ip_discovery.write_u32::<BigEndian>(self.ssrc).unwrap();
        let ip = self.ip.as_bytes();
        let mut padded_ip = [0; 64];
        padded_ip[..ip.len()].copy_from_slice(ip);
        ip_discovery.extend_from_slice(&padded_ip);
        ip_discovery.write_u16::<BigEndian>(self.port).unwrap();

        println!("written IP discovery packet, blocking until response");

        socket
            .send(&ip_discovery)
            .expect("failed to send IP discovery packet");
        let mut buf = [0; 2 + 2 + 4 + 64 + 2];

        loop {
            match socket.recv_from(&mut buf) {
                Ok(_) => {
                    break;
                }
                Err(e) => {
                    if e.kind() != std::io::ErrorKind::WouldBlock {
                        panic!("error receiving data: {}", e);
                    }
                }
            }
        }

        let mut ip = [0; 64];
        ip.copy_from_slice(&buf[8..8 + 64]);

        println!("IP discovered!");

        let port = u16::from_be_bytes([buf[buf.len() - 2], buf[buf.len() - 1]]);
        let ip_info = IPInfo { ip, port };
        self.discovered_ip = Some(ip_info);
        self.discovered_ip.as_ref().unwrap()
    }

    pub fn set_secret(&mut self, secret: Vec<u8>) {
        self.secret = Some(secret);
    }
}

impl Drop for VoiceSession {
    fn drop(&mut self) {
        self.poll_thread_killer
            .send(())
            .expect("failed to send kill signal to poll thread");
    }
}

fn is_not_silent(data: &[i16]) -> bool {
    const THRESHOLD: f32 = 0.03;
    let sum_squares: f32 = data
        .iter()
        .map(|&sample| {
            let normalized = sample as f32 / i16::MAX as f32;
            normalized * normalized
        })
        .sum();

    let rms = (sum_squares / data.len() as f32).sqrt();
    rms > THRESHOLD
}
