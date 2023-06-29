pub use ffxiv_client_structs_util as util;
pub use ffxiv_client_structs_generated as generated;

use util::*;

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
