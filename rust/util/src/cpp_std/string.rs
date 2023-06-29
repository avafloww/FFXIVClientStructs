use std::slice;
use std::ptr;

#[repr(C)]
#[derive(Copy, Clone)]
pub struct String {
    // if (length < 16) uses buffer else uses buffer_ptr
    buffer: [u8; 16],
    length: u64,
    capacity: u64,
}

impl String {
    pub unsafe fn buffer_ptr(&self) -> *const u8 {
        if self.length < 16 {
            self.buffer.as_ptr()
        } else {
            // assuming that the buffer_ptr field is actually a pointer to some memory outside of this struct.
            ptr::read_unaligned(self as *const _ as *const *const u8)
        }
    }

    pub unsafe fn get_bytes(&self) -> Vec<u8> {
        let buffer_ptr = self.buffer_ptr();
        let bytes = slice::from_raw_parts(buffer_ptr, self.length as usize);
        Vec::from(bytes)
    }

    pub fn to_string(&self) -> std::string::String {
        unsafe {
            std::string::String::from_utf8_lossy(self.get_bytes().as_slice()).to_string()
        }
    }
}
