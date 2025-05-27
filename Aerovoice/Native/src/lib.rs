use std::sync::LazyLock;

use cpal::{Device, Host, traits::HostTrait as _};

mod char_ext;
mod crypto;
mod external;
mod rtp;
mod session;
mod snowflake;

pub static DEFAULT_HOST: LazyLock<Host> = LazyLock::new(cpal::default_host);

pub static OUTPUT_DEVICE: LazyLock<Device> = LazyLock::new(|| {
    DEFAULT_HOST
        .default_output_device()
        .expect("failed to get default output device")
});

pub static INPUT_DEVICE: LazyLock<Device> = LazyLock::new(|| {
    DEFAULT_HOST
        .default_input_device()
        .expect("failed to get default output device")
});
