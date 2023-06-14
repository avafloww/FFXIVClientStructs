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
    generated::resolve_vtables(&vtable_resolver);
    generated::resolve_static_addresses(&static_address_resolver);
    generated::resolve_member_functions(&member_function_resolver);
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
pub struct Signature(pub &'static str);

//
// Member functions
//

/// Represents a signature used to resolve a function.
pub struct MemberFunctionSignature {
    pub signature: Signature,
}

impl MemberFunctionSignature {
    pub(crate) const fn new(signature: &'static str) -> Self {
        Self {
            signature: Signature(signature),
        }
    }
}

pub trait ResolvableMemberFunction: Addressable {
    /// The signature of this function.
    const SIGNATURE: MemberFunctionSignature;
}

pub type MemberFunctionResolver = fn(&MemberFunctionSignature) -> Option<*const usize>;

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
    pub(crate) const fn new(signature: &'static str, offset: isize, is_pointer: bool) -> Self {
        Self {
            signature: Signature(signature),
            offset,
            is_pointer,
        }
    }
}

pub trait ResolvableStaticAddress: Addressable {
    /// The signature of this static address.
    const SIGNATURE: StaticAddressSignature;
}

pub type StaticAddressResolver = fn(&StaticAddressSignature) -> Option<*const usize>;

//
// Virtual function tables (vtables)
//

/// Represents a signature used to resolve a vtable.
pub type VTableSignature = StaticAddressSignature;
pub trait ResolvableVTable: Addressable {
    /// The signature of this vtable.
    const SIGNATURE: VTableSignature;
}
pub type VTableResolver = fn(&VTableSignature) -> Option<*const usize>;

// for functions identified by their index in a vtable
pub trait ResolvableVirtualFunction: Addressable {
    /// The index of this function in the parent vtable.
    const VIRTUAL_INDEX: usize;

    /// Returns the vtable address of this function's owning type.
    fn vtable_address() -> Option<*const usize>;
}
