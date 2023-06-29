macro_rules! get_offset {
    ($type:ty, $field:tt) => {{
        let dummy = ::std::mem::MaybeUninit::<$type>::uninit();

        let dummy_ptr = dummy.as_ptr();
        let member_ptr = unsafe { ::std::ptr::addr_of!((*dummy_ptr).$field) };

        member_ptr as usize - dummy_ptr as usize
    }};
}

#[macro_export]
macro_rules! assert_size {
    ($type:ty, $size:expr) => {{
        let asserted_size = ::std::mem::size_of::<$type>();
        assert_eq!(
            asserted_size,
            $size,
            "expected size of {} to be {:#X}, but is actually {:#X}",
            stringify!($type),
            $size,
            asserted_size
        );
    }};
}

#[macro_export]
macro_rules! assert_offset {
    ($type:ty, $field:tt, $offset:expr) => {{
        let asserted_offset = get_offset!($type, $field);
        assert_eq!(
            asserted_offset,
            $offset,
            "expected offset of {}.{} to be {:#X}, but is actually {:#X}",
            stringify!($type),
            stringify!($field),
            $offset,
            asserted_offset
        );
    }};
}

// combines get_offset and assert_offset
// usage:
// assert_offsets!(SomeStruct, [
//     field1: 0x0,
//     field2: 0x4,
//     field3: 0x8,
// ])
#[macro_export]
macro_rules! assert_offsets {
    // case with fields
    ($type:ty, [$($field:tt: $offset:expr,)*]) => {{
        let dummy = ::std::mem::MaybeUninit::<$type>::uninit();
        let dummy_ptr = dummy.as_ptr();

        $(
            {
                let asserted_offset = unsafe { ::std::ptr::addr_of!((*dummy_ptr).$field) } as usize - dummy_ptr as usize;
                assert_eq!(
                    asserted_offset,
                    $offset,
                    "expected offset of {}.{} to be {:#X}, but is actually {:#X}",
                    stringify!($type),
                    stringify!($field),
                    $offset,
                    asserted_offset
                );
            }
        )*
    }};
}
