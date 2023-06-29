use litrs::Literal;
use quote::quote;

const GENERATED_CRATE_NAME: &str = "ffxiv_client_structs_generated";

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

            let sig_struct = if std::env::var("CARGO_PKG_NAME").unwrap() == GENERATED_CRATE_NAME {
                quote! { Signature }
            } else {
                quote! { ::ffxiv_client_structs::util::Signature }
            };
            
            let output = quote! {
                #sig_struct::new(#bytes, #mask)
            };

            output.into()
        }
        Ok(_other) => panic!("expected string literal")
    }
}
