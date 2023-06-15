use litrs::Literal;
use quote::quote;

const BASE_CRATE_NAME: &str = "ffxivclientstructs";

#[proc_macro]
pub fn signature(item: proc_macro::TokenStream) -> proc_macro::TokenStream {
    let input = item.into_iter().next().expect("expected string literal");

    match Literal::try_from(input) {
        Err(e) => return e.to_compile_error(),
        Ok(Literal::String(s)) => {
            let s = s.value();

            let mut bytes = Vec::<u8>::new();
            let mut mask = Vec::<u8>::new();

            for byte in s.split(' ') {
                if byte == "??" {
                    bytes.push(0x00);
                    mask.push(0x00);
                } else {
                    bytes.push(u8::from_str_radix(byte, 16).unwrap());
                    mask.push(0xFF);
                }
            }

            let bytes = bytes.as_slice();
            let mask = mask.as_slice();

            let bytes = quote! { &[#(#bytes),*] };
            let mask = quote! { &[#(#mask),*] };

            let prefix = if std::env::var("CARGO_PKG_NAME").unwrap() == BASE_CRATE_NAME {
                quote! { crate }
            } else {
                quote! { ::ffxivclientstructs }
            };
            
            let output = quote! {
                #prefix::Signature::new(#s, #bytes, #mask)
            };

            output.into()
        }
        Ok(_other) => panic!("expected string literal")
    }
}