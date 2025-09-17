use std::ffi::c_char;

pub trait CCharExt {
    fn to_string_lossy(&self) -> Option<String>;
}

impl CCharExt for *const c_char {
    fn to_string_lossy(&self) -> Option<String> {
        if self.is_null() {
            return None;
        }
        let str = unsafe {
            std::ffi::CStr::from_ptr(*self)
                .to_string_lossy()
                .into_owned()
        };

        Some(str)
    }
}
