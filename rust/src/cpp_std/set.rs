#[repr(C)]
#[derive(Clone)]
pub struct Node<K> where K: Copy {
    left: *mut Node<K>,
    parent: *mut Node<K>,
    right: *mut Node<K>,
    color: u8,
    is_nil: bool,
    _18: u8,
    _19: u8,
    key: K,
}

impl<K> Node<K> where K: Copy {
    unsafe fn next(&mut self) -> *mut Node<K> {
        assert!(!self.is_nil);
        if (*self.right).is_nil {
            let mut ptr: *mut Node<K> = self;
            let mut node: *mut Node<K>;
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

    unsafe fn prev(&mut self) -> *mut Node<K> {
        if self.is_nil {
            return self.right;
        }

        if (*self.left).is_nil {
            let ptr: *mut Node<K> = self;
            let mut node: *mut Node<K>;
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
pub struct Set<K> where K: Copy {
    head: *mut Node<K>,
    count: u64,
}

impl<K> Set<K> where K: Copy {
    fn smallest_value(&self) -> *mut Node<K> {
        unsafe { (*self.head).left }
    }

    fn largest_value(&self) -> *mut Node<K> {
        unsafe { (*self.head).right }
    }

    fn get_enumerator(&self) -> Enumerator<K> {
        Enumerator {
            head: self.head,
            current: self.head,
        }
    }
}

#[repr(C)]
#[derive(Clone)]
pub struct Enumerator<K> where K: Copy {
    head: *mut Node<K>,
    current: *mut Node<K>,
}

impl<K> Enumerator<K> where K: Copy {
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

    unsafe fn current(&self) -> *const K {
        &(*self.current).key as *const K
    }
}