#![allow(unused)]
pub mod address;

// Expose our own bindings to the C++ STL structures used by the game.
pub mod cpp_std;

pub mod macros;

use crate::address::Addressable;
use std::collections::HashMap;
use std::fmt::Display;
use std::sync::RwLock;

/// Represents a signature.
#[derive(Copy, Clone)]
pub struct Signature {
    /// The signature in byte format.
    /// example: &[0xE8, 0x00, 0x00, 0x00, 0x00, 0x84, 0xC0, 0x74, 0x0D, 0xB0, 0x02]
    pub bytes: &'static [u8],

    /// The mask used to ignore bytes in the signature.
    /// example: &[0xFF, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF]
    pub mask: &'static [u8],
}

/// Represents a resolved signature.
#[derive(Copy, Clone)]
pub struct ResolvedSignature {
    pub address: *const u8,
    pub signature: Signature,
}

impl Signature {
    pub const fn new(bytes: &'static [u8], mask: &'static [u8]) -> Self {
        Self {
            bytes,
            mask,
        }
    }
}

/// A `Display` implementation for `Signature` that generates a string representation of the signature.
impl Display for Signature {
    fn fmt(&self, f: &mut std::fmt::Formatter<'_>) -> std::fmt::Result {
        let mut string = String::new();
        for (i, byte) in self.bytes.iter().enumerate() {
            if i > 0 {
                string.push_str(" ");
            }
            
            if self.mask[i] == 0x00 {
                string.push_str("??");
            } else {
                string.push_str(&format!("{:02X}", byte));
            }
        }
        
        write!(f, "{}", string)
    }
}

//
// Member functions
//

/// Represents a signature used to resolve a function.
pub struct MemberFunctionSignature {
    pub signature: Signature,
}

impl MemberFunctionSignature {
    pub const fn new(signature: Signature) -> Self {
        Self { signature }
    }
}

pub trait ResolvableMemberFunction: Addressable {
    /// The signature of this function.
    const SIGNATURE: MemberFunctionSignature;
}

pub type MemberFunctionResolver = unsafe fn(&MemberFunctionSignature) -> *const u8;

//
// Static addresses
//

/// Represents a signature used to resolve a static address or vtable base.
pub struct StaticAddressSignature {
    pub signature: Signature,
    pub offset: isize,
    pub is_pointer: bool,
}

impl StaticAddressSignature {
    pub const fn new(signature: Signature, offset: isize, is_pointer: bool) -> Self {
        Self {
            signature,
            offset,
            is_pointer,
        }
    }
}

pub trait ResolvableStaticAddress: Addressable {
    /// The signature of this static address.
    const SIGNATURE: StaticAddressSignature;
}

pub type StaticAddressResolver = unsafe fn(&StaticAddressSignature) -> *const u8;

//
// Virtual function tables (vtables)
//

/// Represents a signature used to resolve a vtable.
pub type VTableSignature = StaticAddressSignature;
pub trait ResolvableVTable: Addressable {
    /// The signature of this vtable.
    const SIGNATURE: VTableSignature;
}
pub type VTableResolver = unsafe fn(&VTableSignature) -> *const u8;

// for functions identified by their index in a vtable
pub trait ResolvableVirtualFunction: Addressable {
    /// The index of this function in the parent vtable.
    const VIRTUAL_INDEX: usize;

    /// Returns the vtable address of this function's owning type.
    fn vtable_address() -> *const u8;
}
