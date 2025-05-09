use std::marker::PhantomData;

use byteorder::{BigEndian, ReadBytesExt};
use thiserror::Error;

use crate::crypto::Cryptor;

#[derive(Debug)]
pub struct Encrypted;

#[derive(Debug)]
pub struct Decrypted;

#[derive(Debug)]
pub struct RtpPacket<T> {
    pub version_flags: u8,
    pub payload_type: u8,
    pub sequence: u16,
    pub timestamp: u32,
    pub ssrc: u32,
    payload: Vec<u8>,
    raw: Vec<u8>,
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

#[derive(Debug, Clone, Copy)]
pub enum HeaderExtensionType {
    None,
    Partial,
    Full,
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

        Ok(RtpPacket::<Decrypted> {
            version_flags: self.version_flags,
            payload_type: self.payload_type,
            sequence: self.sequence,
            timestamp: self.timestamp,
            ssrc: self.ssrc,
            payload: decrypted,
            raw: self.raw,
            _marker: PhantomData,
        })
    }

    pub fn encrypted_blob(&self) -> &[u8] {
        &self.raw[self.header_length(HeaderExtensionType::Partial)..]
    }
}

impl RtpPacket<Decrypted> {
    pub fn payload(&self) -> &[u8] {
        &self.payload
    }
}
