#[repr(C)]
#[derive(Clone)]
pub struct Deque<T> {
    pub container_base: *mut (), // iterator base nonsense
    pub map: *mut *mut T, // pointer to array of pointers (size MapSize) to arrays of T (size BlockSize)
    pub map_size: u64, // size of map
    pub my_off: u64, // offset of current first element
    pub my_size: u64, // current length 
}

impl<T> Deque<T> {
    pub fn block_size() -> usize {
        let size = std::mem::size_of::<T>();
        if size <= 1 {
            16
        } else if size <= 2 {
            8
        } else if size <= 4 {
            4
        } else if size <= 8 {
            2
        } else {
            1
        }
    }

    pub fn get_block(&self, offset: u64) -> u64 {
        (offset / (Self::block_size() as u64)) & (self.map_size - 1)
    }

    pub unsafe fn get(&self, index: u64) -> Option<T> {
        if index >= self.my_size {
            return None;
        }

        let actual_index = self.my_off + index;
        let block = self.get_block(actual_index);
        let offset = actual_index % (Self::block_size() as u64);
        let p = *self.map.offset(block as isize);
        Some(std::ptr::read(p.offset(offset as isize)))
    }
}
