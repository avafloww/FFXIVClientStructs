use crate::cpp_std::Pair;

#[repr(C)]
#[derive(Clone)]
pub struct Node<K, V> {
    left: *mut Node<K, V>,
    parent: *mut Node<K, V>,
    right: *mut Node<K, V>,
    color: u8,
    is_nil: bool,
    _18: u8,
    _19: u8,
    key_value_pair: Pair<K, V>,
}

impl<K, V> Node<K, V> {
    unsafe fn next(&mut self) -> *mut Node<K, V> {
        assert!(!self.is_nil);
        if (*self.right).is_nil {
            let mut ptr: *mut Node<K, V> = self;
            let mut node: *mut Node<K, V>;
            while {
                node = (*ptr).parent;
                !(*node).is_nil && ptr == (*node).right
            } {
                ptr = node;
            }
            node
        } else {
            let mut ret = self.right;
            while !(*(*ret).left).is_nil {
                ret = (*ret).left;
            }
            ret
        }
    }

    unsafe fn prev(&mut self) -> *mut Node<K, V> {
        if self.is_nil {
            return self.right;
        }

        if (*self.left).is_nil {
            let ptr: *mut Node<K, V> = self;
            let mut node: *mut Node<K, V>;
            while {
                node = (*ptr).parent;
                !(*node).is_nil && ptr == (*node).left
            } {
                // ptr = node;
                
            }
            if (*ptr).is_nil {
                ptr
            } else {
                node
            }
        } else {
            let mut ret = self.left;
            while !(*((*ret).right)).is_nil {
                ret = (*ret).right;
            }
            ret
        }
    }
}

#[repr(C)]
#[derive(Clone)]
pub struct Map<K, V> {
    head: *mut Node<K, V>,
    count: u64,
}

impl<K, V> Map<K, V> {
    fn smallest_value(&self) -> *mut Node<K, V> {
        unsafe { (*self.head).left }
    }

    fn largest_value(&self) -> *mut Node<K, V> {
        unsafe { (*self.head).right }
    }

    fn get_enumerator(&self) -> Enumerator<K, V> {
        Enumerator {
            head: self.head,
            current: self.head,
        }
    }
}

#[repr(C)]
#[derive(Clone)]
pub struct Enumerator<K, V> {
    head: *mut Node<K, V>,
    current: *mut Node<K, V>,
}

impl<K, V> Enumerator<K, V> {
    fn move_next(&mut self) -> bool {
        if self.current.is_null() || self.current == unsafe { (*self.head).right } {
            return false;
        }
        self.current = if self.current == self.head {
            unsafe { (*self.head).left }
        } else {
            unsafe { (*self.current).next() }
        };
        !self.current.is_null()
    }

    unsafe fn current(&self) -> *const Pair<K, V> {
        &(*self.current).key_value_pair as *const Pair<K, V>
    }
}
