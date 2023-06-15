#![allow(unused)]

pub mod address;

// Expose our own bindings to the C++ STL structures used by the game.
pub mod cpp_std;

// Include and expose generated bindings.
mod generated;
mod internal;

use std::collections::HashMap;
use std::sync::RwLock;
pub use generated::ffxiv;
pub use generated::havok;
use crate::address::Addressable;

// Public API to resolve addresses for all addressable types we support.
pub unsafe fn resolve_all(
    vtable_resolver: VTableResolver,
    static_address_resolver: StaticAddressResolver,
    member_function_resolver: MemberFunctionResolver,
) {
    generated::resolve_vtables(vtable_resolver);
    generated::resolve_static_addresses(static_address_resolver);
    generated::resolve_member_functions(member_function_resolver);
}

#[cfg(feature = "async-resolution")]
pub async unsafe fn resolve_all_async(
    vtable_resolver: VTableResolver,
    static_address_resolver: StaticAddressResolver,
    member_function_resolver: MemberFunctionResolver,
) {
    generated::resolve_vtables_async(vtable_resolver).await;
    generated::resolve_static_addresses_async(static_address_resolver).await;
    generated::resolve_member_functions_async(member_function_resolver).await;
}


/// Represents a signature.
#[derive(Copy, Clone)]
pub struct Signature {
    /// The signature in string format.
    /// example: "E8 ?? ?? ?? ?? 84 C0 74 0D B0 02"
    pub string: &'static str,

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
    pub const fn new(string: &'static str, bytes: &'static [u8], mask: &'static [u8]) -> Self {
        Self {
            string,
            bytes,
            mask,
        }
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
    pub(crate) const fn new(signature: Signature) -> Self {
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
    pub(crate) const fn new(signature: Signature, offset: isize, is_pointer: bool) -> Self {
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
