mod generated;
pub use generated::*;

pub mod cpp_std;

pub trait GameFunction {
    /// Returns a string containing the signature of this function.
    /// If this function does not have a signature, returns `None`.
    fn signature() -> Option<&'static str>;
}
