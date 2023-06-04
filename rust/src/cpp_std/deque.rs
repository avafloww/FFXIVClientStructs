#[repr(C)]
#[derive(Clone)]
pub struct Deque {
    pub container_base: *mut (), // iterator base nonsense
    pub map: *mut *mut u8, // pointer to array of pointers (size MapSize) to arrays of u8 (size BlockSize)
    pub map_size: u64, // size of map
    pub my_off: u64, // offset of current first element
    pub my_size: u64, // current length 
}

impl Deque {
    pub fn block_size() -> usize {
        let size = std::mem::size_of::<u8>();
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
        (offset / (Deque::block_size() as u64)) & (self.map_size - 1)
    }

    pub unsafe fn get(&self, index: u64) -> Option<u8> {
        if index >= self.my_size {
            return None;
        }

        let actual_index = self.my_off + index;
        let block = self.get_block(actual_index);
        let offset = actual_index % (Deque::block_size() as u64);
        Some(*(*self.map.offset(block as isize)).offset(offset as isize))
    }
}
