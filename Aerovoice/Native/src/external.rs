use std::ffi::c_char;

use crate::{
    char_ext::CCharExt,
    crypto::encryption_by_priority,
    session::{IPInfo, VoiceSession},
    snowflake::Snowflake,
};

#[unsafe(no_mangle)]
extern "C" fn voice_session_new(
    ssrc: u32,
    channel: u64,
    ip: *const c_char,
    port: u16,
    on_speaking: extern "C" fn(u32, bool),
) -> *mut VoiceSession {
    let channel = Snowflake::from_u64(channel);

    let Some(ip) = ip.to_string_lossy() else {
        return std::ptr::null_mut();
    };

    let session = VoiceSession::new(ssrc, channel, ip, port, on_speaking);

    Box::into_raw(Box::new(session))
}

// extern "C" fn on_speaking(ssrc: u32, speaking: bool) {
//     println!("SSRC: {}, Speaking: {}", ssrc, speaking);
// }

#[unsafe(no_mangle)]
extern "C" fn voice_session_init_poll_thread(session: *mut VoiceSession) {
    if session.is_null() {
        return;
    }

    let session = unsafe { &mut *session };
    session.init_poll_thread();
}

#[unsafe(no_mangle)]
extern "C" fn voice_session_set_secret(session: *mut VoiceSession, secret: *const u8, len: u32) {
    if session.is_null() {
        return;
    }

    let session = unsafe { &mut *session };
    let len = len as usize;
    let secret = unsafe { std::slice::from_raw_parts(secret, len) };
    let secret = secret.to_vec();
    session.set_secret(secret);
}

#[unsafe(no_mangle)]
extern "C" fn voice_session_select_cryptor(
    session: *mut VoiceSession,
    available_methods: *const *const c_char,
    available_methods_len: u32,
) -> *mut c_char {
    if session.is_null() {
        return std::ptr::null_mut();
    }

    let session = unsafe { &mut *session };

    let available_methods =
        unsafe { std::slice::from_raw_parts(available_methods, available_methods_len as usize) };
    // convert to Vec<String>
    let available_methods = available_methods
        .iter()
        .map(|&s| unsafe { std::ffi::CStr::from_ptr(s) })
        .map(|s| s.to_string_lossy().to_string())
        .collect::<Vec<_>>();

    let Some(send_cryptor) = encryption_by_priority(&available_methods) else {
        println!("!!! WARNING !!! No encryption method was found. VOICE CHAT WILL NOT WORK!");
        return std::ptr::null_mut();
    };

    let Some(recv_cryptor) = encryption_by_priority(&available_methods) else {
        println!("!!! WARNING !!! No encryption method was found. VOICE CHAT WILL NOT WORK!");
        return std::ptr::null_mut();
    };

    let name = recv_cryptor.name().to_string();
    let name = std::ffi::CString::new(name).unwrap();

    session.set_cryptor(send_cryptor, recv_cryptor);

    name.into_raw()
}

#[unsafe(no_mangle)]
extern "C" fn voice_session_discover_ip(session: *mut VoiceSession) -> *const IPInfo {
    if session.is_null() {
        return std::ptr::null();
    }

    let session = unsafe { &mut *session };
    let ip_info = session.discover_ip();

    ip_info as *const _
}

#[unsafe(no_mangle)]
extern "C" fn voice_session_free(session: *mut VoiceSession) {
    if session.is_null() {
        return;
    }

    unsafe {
        drop(Box::from_raw(session));
    }
}
