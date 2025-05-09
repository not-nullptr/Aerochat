use crate::{
    crypto::Cryptor,
    rtp::{Encrypted, RtpPacket},
    snowflake::Snowflake,
};
use audiopus::{Channels, SampleRate, coder::Decoder};
use byteorder::{BigEndian, WriteBytesExt};

use cpal::{
    Device, OutputCallbackInfo, Stream, StreamConfig,
    traits::{DeviceTrait, HostTrait, StreamTrait as _},
};
use rtrb::{Consumer, Producer, RingBuffer};
use std::{
    collections::{HashMap, hash_map::Entry},
    net::UdpSocket,
    sync::{
        LazyLock,
        mpsc::{self, Receiver, Sender},
    },
    thread::{self, JoinHandle},
};

static DEVICE: LazyLock<Device> = LazyLock::new(|| {
    let host = cpal::default_host();
    host.default_output_device()
        .expect("failed to get default output device")
});

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
    cryptor: Option<Box<dyn Cryptor>>,
    poll_thread: JoinHandle<()>,
    poll_thread_initialiser: Sender<ThreadInitialiser>,
    poll_thread_killer: Sender<()>,
    packet_sender: Sender<RtpPacket<Encrypted>>,
    on_speaking: extern "C" fn(u32, bool),
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
            cryptor: None,
            poll_thread,
            poll_thread_initialiser: init_tx,
            packet_sender: packet_tx,
            poll_thread_killer: killer_tx,
            on_speaking,
        }
    }

    pub fn init_poll_thread(&mut self) {
        let socket = self.socket.take().expect("socket is not set");
        let cryptor = self.cryptor.take().expect("cryptor is not set");
        let secret = self.secret.take().expect("secret is not set");

        let thread_initialiser = ThreadInitialiser {
            cryptor,
            secret,
            socket,
        };

        self.poll_thread_initialiser
            .send(thread_initialiser)
            .expect("failed to send initialiser to poll thread");
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
        loop {
            if killer_rx.try_recv().is_ok() {
                println!("killing poll thread");
                break;
            }

            if let Ok(packet) = packet_rx.try_recv() {
                println!("TODO: handle packet from sender thread ({})", packet.ssrc);
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

            let Ok(packet) = RtpPacket::parse(packet) else {
                println!("failed to parse packet");
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

                let stream = DEVICE.build_output_stream(
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

    pub fn set_cryptor(&mut self, cryptor: Box<dyn Cryptor>) {
        self.cryptor = Some(cryptor);
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
