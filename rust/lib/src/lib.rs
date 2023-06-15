#![allow(unused)]

// Expose our own bindings to the C++ STL structures used by the game.
pub mod cpp_std;

// Include and expose generated bindings.
mod generated;

pub use generated::ffxiv;
pub use generated::havok;

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

/// Represents a type that can be resolved to an address in the game's memory space.
pub trait Addressable {
    /// Returns the resolved address of this type.
    /// If the type is unresolved, `None` is returned.
    fn address() -> Option<*const usize>
    where
        Self: Sized;
}

/// Internal trait used to set the address of a type.
pub(crate) trait AddressableMut: Addressable {
    fn set_address(address: &Option<*const usize>)
    where
        Self: Sized;
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

impl Signature {
    pub const fn new(string: &'static str, bytes: &'static [u8], mask: &'static [u8]) -> Self {
        Self {
            string,
            bytes,
            mask,
        }
    }

    // pub fn from_string(string: &'a str) -> Self {
    //     let mut bytes = Vec::<u8>::new();
    //     let mut mask = Vec::<u8>::new();
    //
    //     for byte in string.split(' ') {
    //         if byte == "??" {
    //             bytes.push(0x00);
    //             mask.push(0x00);
    //         } else {
    //             bytes.push(u8::from_str_radix(byte, 16).unwrap());
    //             mask.push(0xFF);
    //         }
    //     }
    //
    //     let bytes: [u8] = [9];
    //     let mask: Vec<u8> = mask.as_slice().to_owned();
    //
    //     Self {
    //         string,
    //         bytes: *bytes,
    //         mask: *mask,
    //     }
    // }
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

pub type MemberFunctionResolver = unsafe fn(&MemberFunctionSignature) -> Option<*const usize>;

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

pub type StaticAddressResolver = unsafe fn(&StaticAddressSignature) -> Option<*const usize>;

//
// Virtual function tables (vtables)
//

/// Represents a signature used to resolve a vtable.
pub type VTableSignature = StaticAddressSignature;
pub trait ResolvableVTable: Addressable {
    /// The signature of this vtable.
    const SIGNATURE: VTableSignature;
}
pub type VTableResolver = unsafe fn(&VTableSignature) -> Option<*const usize>;

// for functions identified by their index in a vtable
pub trait ResolvableVirtualFunction: Addressable {
    /// The index of this function in the parent vtable.
    const VIRTUAL_INDEX: usize;

    /// Returns the vtable address of this function's owning type.
    fn vtable_address() -> Option<*const usize>;
}
