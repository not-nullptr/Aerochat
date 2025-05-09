mod aead_xchacha20_poly1305_rtpsize;

pub use aead_xchacha20_poly1305_rtpsize::AeadXChaCha20Poly1305RtpSize;

use crate::rtp::{Encrypted, RtpPacket};

const CRYPTOR_PRIORITY: &[&str] = &["aead_xchacha20_poly1305_rtpsize"];

pub fn encryption_by_priority(available_methods: &[String]) -> Option<Box<dyn Cryptor>> {
    for name in CRYPTOR_PRIORITY {
        if available_methods.contains(&name.to_string()) {
            return match *name {
                "aead_xchacha20_poly1305_rtpsize" => Some(Box::new(AeadXChaCha20Poly1305RtpSize)),
                _ => None,
            };
        }
    }

    None
}

pub trait Cryptor: Send + Sync {
    fn name(&self) -> &'static str;
    fn encrypt(&mut self, packet: &RtpPacket<Encrypted>, key: &[u8]) -> Option<Vec<u8>>;
    fn decrypt(&mut self, packet: &RtpPacket<Encrypted>, key: &[u8]) -> Option<Vec<u8>>;
}
