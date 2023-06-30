#[repr(C, align(8))]
#[derive(Copy, Clone)]
pub struct Pair<T1, T2>
{
    item1: T1,
    item2: T2,
}
