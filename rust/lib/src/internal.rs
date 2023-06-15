// Internal helper functions.

use crate::{MemberFunctionResolver, MemberFunctionSignature, StaticAddressResolver, StaticAddressSignature, VTableResolver, VTableSignature};
use crate::address::{get_address, set_address};

pub unsafe fn resolve_vtable(key: &str, signature: &VTableSignature, resolver: VTableResolver) {
    let address = resolver(signature);
    set_address(key, address);
}

#[cfg(feature = "async-resolution")]
pub async unsafe fn resolve_vtable_async(key: &str, signature: &VTableSignature, resolver: VTableResolver) {
    resolve_vtable(key, signature, resolver)
}

pub unsafe fn resolve_static_address(key: &str, signature: &StaticAddressSignature, resolver: StaticAddressResolver) {
    let address = resolver(signature);
    set_address(key, address);
}

#[cfg(feature = "async-resolution")]
pub async unsafe fn resolve_static_address_async(key: &str, signature: &StaticAddressSignature, resolver: StaticAddressResolver) {
    resolve_static_address(key, signature, resolver)
}

pub unsafe fn resolve_member_function(key: &str, signature: &MemberFunctionSignature, resolver: MemberFunctionResolver) {
    let address = resolver(signature);
    set_address(key, address);
}

#[cfg(feature = "async-resolution")]
pub async unsafe fn resolve_member_function_async(key: &str, signature: &MemberFunctionSignature, resolver: MemberFunctionResolver) {
    resolve_member_function(key, signature, resolver)
}

pub unsafe fn resolve_virtual_function(key: &str, vtable_key: &str, virtual_index: usize) {
    let vtable_address = get_address(vtable_key) as *const usize;
    if vtable_address.is_null() {
        panic!("unresolved vtable: {}", vtable_key);
    }

    let address = vtable_address.offset(virtual_index as isize);
    set_address(key, address as *const u8);
}

#[cfg(feature = "async-resolution")]
pub async unsafe fn resolve_virtual_function_async(key: &str, vtable_key: &str, virtual_index: usize) {
    resolve_virtual_function(key, vtable_key, virtual_index)
}
