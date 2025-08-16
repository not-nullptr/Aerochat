use crate::rtp::{Decrypted, Encrypted, HeaderExtensionType, RtpPacket};

use super::Cryptor;
use chacha20poly1305::{
    KeyInit, XChaCha20Poly1305,
    aead::{AeadMut, Payload},
};

const NONCE_BYTES: usize = 4;

#[derive(Default, Clone, Copy)]
pub struct AeadXChaCha20Poly1305RtpSize {
    sequence: u32,
}

impl Cryptor for AeadXChaCha20Poly1305RtpSize {
    fn decrypt(&mut self, packet: &RtpPacket<Encrypted>, key: &[u8]) -> Option<Vec<u8>> {
        let mut cipher = XChaCha20Poly1305::new(key.into());
        let header = packet.header(HeaderExtensionType::Partial);
        let blob = packet.encrypted_blob();
        if blob.len() < NONCE_BYTES {
            return None;
        }

        let ciphertext = &blob[..blob.len() - NONCE_BYTES];
        let nonce_bytes = &blob[blob.len() - NONCE_BYTES..];
        let mut nonce = [0; 24];
        nonce[..NONCE_BYTES].copy_from_slice(nonce_bytes);
        let payload = Payload {
            msg: ciphertext,
            aad: header,
        };

        cipher.decrypt(&nonce.into(), payload).ok()
    }

    fn encrypt(&mut self, packet: &RtpPacket<Decrypted>, key: &[u8]) -> Option<Vec<u8>> {
        self.sequence = self.sequence.wrapping_add(1);
        let data = packet.data_to_encrypt();
        let mut cipher = XChaCha20Poly1305::new(key.into());
        let header = packet.header(HeaderExtensionType::Partial);
        let mut nonce = [0; 24];
        nonce[..NONCE_BYTES].copy_from_slice(&self.sequence.to_be_bytes());
        let payload = Payload {
            msg: data,
            aad: header,
        };
        let mut ciphertext = cipher.encrypt(&nonce.into(), payload).ok()?;
        ciphertext.extend_from_slice(&nonce[..NONCE_BYTES]);
        Some(ciphertext)
    }

    fn name(&self) -> &'static str {
        "aead_xchacha20_poly1305_rtpsize"
    }
}
