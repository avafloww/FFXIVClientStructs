use std::ptr;
use std::ops::Index;
use std::slice;

#[repr(C)]
#[derive(Clone)]
pub struct Vector<T> {
    first: *mut T,
    last: *mut T,
    end: *mut T,
}

impl<T> Vector<T> {
    pub unsafe fn new() -> Self {
        Vector {
            first: ptr::null_mut(),
            last: ptr::null_mut(),
            end: ptr::null_mut(),
        }
    }

    pub unsafe fn size(&self) -> usize {
        if self.first.is_null() || self.last.is_null() {
            return 0;
        }

        (self.last.offset_from(self.first) as usize) / std::mem::size_of::<T>()
    }

    pub unsafe fn capacity(&self) -> usize {
        if self.end.is_null() || self.first.is_null() {
            return 0;
        }

        (self.end.offset_from(self.first) as usize) / std::mem::size_of::<T>()
    }

    pub unsafe fn get(&self, index: usize) -> Option<&T> {
        if index >= self.size() {
            return None;
        }

        self.first.offset(index as isize).as_ref()
    }

    pub unsafe fn as_slice(&self) -> &[T] {
        let size = self.size();
        if size > 0x7FEFFFFF {
            panic!("Size exceeds max. Array index. (Size={})", size);
        }

        slice::from_raw_parts(self.first, self.size())
    }
}

impl<T> Index<usize> for Vector<T> {
    type Output = T;

    fn index(&self, index: usize) -> &Self::Output {
        unsafe { self.get(index).expect("Index out of Range") }
    }
}
