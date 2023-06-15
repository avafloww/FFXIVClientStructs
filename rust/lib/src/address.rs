use std::cell::OnceCell;
use std::collections::HashMap;
use std::ops::Add;
use std::sync::RwLock;
use log::debug;

/// Track resolved addresses in a key => address map, as a static variable.
static mut ADDRESSES: RwLock<OnceCell<HashMap<String, *const u8>>> = RwLock::new(OnceCell::new());

fn get_address_inner(key: &str) -> *const u8 {
    let addresses = unsafe { ADDRESSES.read().unwrap() };
    match addresses.get() {
        Some(map) => map.get(key).unwrap().clone(),
        None => std::ptr::null(),
    }
}

pub fn get<T: Addressable>() -> *const u8 {
    get_address_inner(T::KEY)
}

pub fn get_address(key: &str) -> *const u8 {
    get_address_inner(key)
}

fn set_address_inner(key: &str, address: *const u8) {
    if address.is_null() {
        return;
    }

    let mut addresses = unsafe { ADDRESSES.write().unwrap() };
    match addresses.get_mut() {
        Some(map) => {
            map.insert(key.to_owned(), address);
        }
        None => {
            let mut map = HashMap::new();
            map.insert(key.to_owned(), address);
            addresses.set(map).expect("failed to initialize address map");
        }
    };

    debug!("set_address: {} @ {:p}", key, address);
}

pub(crate) fn set<T: Addressable>(address: *const u8) {
    set_address_inner(T::KEY, address);
}

pub(crate) fn set_address(key: &str, address: *const u8) {
    set_address_inner(key, address);
}

/// Represents a type that can be resolved to an address in the game's memory space.
pub trait Addressable {
    /// A unique key used to identify this type.
    const KEY: &'static str;

    /// Returns the resolved address of this type.
    /// If the type is unresolved, std::ptr::null() is returned.
    fn address() -> *const u8 where Self: Sized;
}