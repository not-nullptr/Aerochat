use std::marker::PhantomData;

use byteorder::{BigEndian, ReadBytesExt, WriteBytesExt};
use thiserror::Error;

use crate::crypto::Cryptor;

#[derive(Debug, PartialEq, Eq, Clone)]
pub struct Encrypted;

#[derive(Debug, PartialEq, Eq, Clone)]
pub struct Decrypted;

#[derive(Debug, PartialEq, Eq, Clone)]
pub struct RtpPacket<T> {
    pub version_flags: u8,
    pub payload_type: u8,
    pub sequence: u16,
    pub timestamp: u32,
    pub ssrc: u32,
    payload: Vec<u8>,
    raw: Vec<u8>,
    is_silence: Option<bool>,
    _marker: PhantomData<T>,
}

#[derive(Debug, Error)]
pub enum PacketParseError {
    #[error("Invalid packet length: {0}")]
    PacketTooShort(usize),
    #[error("Invalid header:: {0}")]
    InvalidHeader(#[from] std::io::Error),
}

#[derive(Debug, Error)]
pub enum PacketDecryptError {
    #[error("Decryption failed for cryptor {0}")]
    DecryptionFailed(&'static str),
}

#[derive(Debug, Error)]
pub enum PacketEncryptError {
    #[error("Encryption failed for cryptor {0}")]
    EncryptionFailed(&'static str),
}

#[derive(Debug, Clone, Copy)]
pub enum HeaderExtensionType {
    None,
    Partial,
    Full,
}

#[derive(Debug, Error)]
pub enum PacketConstructError {
    #[error("Failed to construct packet")]
    PacketConstructFailed,
}

impl<T> RtpPacket<T> {
    pub fn header(&self, header_type: HeaderExtensionType) -> &[u8] {
        &self.raw[..self.header_length(header_type)]
    }

    pub fn extension_length(&self) -> usize {
        if (self.version_flags & 0x10) == 0 {
            return 0;
        }

        let mut packet = &self.raw[14..];
        let extension_length = packet.read_u16::<BigEndian>().unwrap_or(0) as usize;

        extension_length * 4
    }

    pub fn header_length(&self, header_type: HeaderExtensionType) -> usize {
        const HEADER_LENGTH: usize = 12;
        if !self.has_extension() {
            return HEADER_LENGTH;
        }

        match header_type {
            HeaderExtensionType::None => HEADER_LENGTH,
            HeaderExtensionType::Partial => HEADER_LENGTH + 4,
            HeaderExtensionType::Full => HEADER_LENGTH + self.extension_length() + 4,
        }
    }

    pub fn has_extension(&self) -> bool {
        (self.version_flags & 0x10) != 0
    }

    pub fn raw(&self) -> &[u8] {
        &self.raw
    }

    pub fn is_silent(&self) -> Option<bool> {
        self.is_silence
    }
}

impl RtpPacket<Encrypted> {
    pub fn parse(packet: impl AsRef<[u8]>) -> Result<Self, PacketParseError> {
        let raw = packet.as_ref().to_vec();
        let mut packet = packet.as_ref();
        if packet.len() < 12 {
            return Err(PacketParseError::PacketTooShort(packet.len()));
        }

        let version_flags = packet.read_u8()?;
        let payload_type = packet.read_u8()?;
        let sequence = packet.read_u16::<BigEndian>()?;
        let timestamp = packet.read_u32::<BigEndian>()?;
        let ssrc = packet.read_u32::<BigEndian>()?;

        Ok(RtpPacket::<Encrypted> {
            version_flags,
            payload_type,
            sequence,
            timestamp,
            ssrc,
            raw,
            payload: vec![],
            _marker: PhantomData,
            is_silence: None,
        })
    }

    pub fn decrypt(
        self,
        cryptor: &mut (impl Cryptor + ?Sized),
        secret: &[u8],
    ) -> Result<RtpPacket<Decrypted>, PacketDecryptError> {
        let mut decrypted = cryptor
            .decrypt(&self, secret)
            .ok_or_else(|| PacketDecryptError::DecryptionFailed(cryptor.name()))?;

        if self.has_extension() {
            decrypted.drain(..self.extension_length());
        }

        let header = self.header(HeaderExtensionType::Partial);

        let mut raw = Vec::with_capacity(header.len() + decrypted.len());
        raw.extend_from_slice(header);
        raw.extend_from_slice(&decrypted);

        Ok(RtpPacket::<Decrypted> {
            version_flags: self.version_flags,
            payload_type: self.payload_type,
            sequence: self.sequence,
            timestamp: self.timestamp,
            ssrc: self.ssrc,
            payload: decrypted,
            raw,
            _marker: PhantomData,
            is_silence: self.is_silence,
        })
    }

    pub fn encrypted_blob(&self) -> &[u8] {
        &self.raw[self.header_length(HeaderExtensionType::Partial)..]
    }
}

impl RtpPacket<Decrypted> {
    pub fn builder() -> RtpPacketBuilder {
        RtpPacketBuilder::default()
    }

    pub fn payload(&self) -> &[u8] {
        &self.payload
    }

    pub fn encrypt(
        self,
        cryptor: &mut (impl Cryptor + ?Sized),
        secret: &[u8],
    ) -> Result<RtpPacket<Encrypted>, PacketEncryptError> {
        let encrypted_payload = cryptor
            .encrypt(&self, secret)
            .ok_or_else(|| PacketEncryptError::EncryptionFailed(cryptor.name()))?;

        let header = self.header(HeaderExtensionType::Partial);
        let mut raw = Vec::with_capacity(header.len() + encrypted_payload.len());
        raw.extend_from_slice(header);
        raw.extend_from_slice(&encrypted_payload);

        Ok(RtpPacket::<Encrypted> {
            version_flags: self.version_flags,
            payload_type: self.payload_type,
            sequence: self.sequence,
            timestamp: self.timestamp,
            ssrc: self.ssrc,
            is_silence: self.is_silence,
            payload: encrypted_payload,
            raw,
            _marker: PhantomData,
        })
    }

    pub fn data_to_encrypt(&self) -> &[u8] {
        &self.raw[self.header_length(HeaderExtensionType::Partial)..]
    }
}

#[derive(Debug)]
pub struct RtpPacketBuilder {
    version_flags: u8,
    payload_type: u8,
    sequence: u16,
    timestamp: u32,
    ssrc: u32,
    payload: Vec<u8>,
    is_silence: Option<bool>,
}

impl Default for RtpPacketBuilder {
    fn default() -> Self {
        Self {
            version_flags: 0x80,
            payload_type: 0x78,
            sequence: 0,
            timestamp: 0,
            ssrc: 0,
            payload: vec![],
            is_silence: None,
        }
    }
}

impl RtpPacketBuilder {
    pub fn silence(mut self, is_silence: bool) -> Self {
        self.is_silence = Some(is_silence);
        self
    }

    pub fn sequence(mut self, sequence: u16) -> Self {
        self.sequence = sequence;
        self
    }

    pub fn timestamp(mut self, timestamp: u32) -> Self {
        self.timestamp = timestamp;
        self
    }

    pub fn ssrc(mut self, ssrc: u32) -> Self {
        self.ssrc = ssrc;
        self
    }

    pub fn payload(mut self, payload: Vec<u8>) -> Self {
        self.payload = payload;
        self
    }

    pub fn build(self) -> Result<RtpPacket<Decrypted>, PacketConstructError> {
        let packet_size = 12 + self.payload.len();
        let mut raw = Vec::with_capacity(packet_size);
        raw.write_u8(self.version_flags)
            .map_err(|_| PacketConstructError::PacketConstructFailed)?;
        raw.write_u8(self.payload_type)
            .map_err(|_| PacketConstructError::PacketConstructFailed)?;
        raw.write_u16::<BigEndian>(self.sequence)
            .map_err(|_| PacketConstructError::PacketConstructFailed)?;
        raw.write_u32::<BigEndian>(self.timestamp)
            .map_err(|_| PacketConstructError::PacketConstructFailed)?;
        raw.write_u32::<BigEndian>(self.ssrc)
            .map_err(|_| PacketConstructError::PacketConstructFailed)?;
        raw.extend_from_slice(&self.payload);

        Ok(RtpPacket::<Decrypted> {
            version_flags: self.version_flags,
            payload_type: self.payload_type,
            sequence: self.sequence,
            timestamp: self.timestamp,
            ssrc: self.ssrc,
            payload: self.payload,
            is_silence: self.is_silence,
            raw,
            _marker: PhantomData,
        })
    }
}

mod tests {
    const SECRET: [u8; 32] = [127; 32];
    use crate::crypto::AeadXChaCha20Poly1305RtpSize;

    use super::*;

    #[test]
    fn encrypt_decrypt() {
        let packet = RtpPacket::builder()
            .ssrc(250)
            .sequence(24)
            .timestamp(100)
            .payload(vec![1, 2, 3, 4, 5, 6, 7, 8])
            .build()
            .expect("failed to construct packet");

        let mut cryptor = AeadXChaCha20Poly1305RtpSize::default();

        let encrypted = packet
            .clone()
            .encrypt(&mut cryptor, &SECRET)
            .expect("failed to encrypt packet");

        let decrypted = encrypted
            .decrypt(&mut cryptor, &SECRET)
            .expect("failed to decrypt packet");

        assert_eq!(packet, decrypted);
    }
}
