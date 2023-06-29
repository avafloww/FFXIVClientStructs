use std::fs::File;
use std::{env, io};
use std::io::{BufRead, BufReader};
use std::path::Path;
use std::process::Command;

fn should_export() -> bool {
    if env::var("CS_RUST_FORCE_EXPORT").is_ok() {
        println!("should_export = true (CS_RUST_FORCE_EXPORT is set)");
        return true;
    }

    let generated_file = env::var("CARGO_MANIFEST_DIR").ok().unwrap();
    let generated_file = Path::new(&generated_file).join("src/generated.rs");
    if !generated_file.exists() {
        println!("should_export = true (generated file did not exist)");
        true
    } else {
        // check the git hash comment at the top of the generated file
        // read only the first line of the file, as it's big
        let file = File::open(&generated_file).unwrap();
        let mut buf = BufReader::new(file);

        let mut line = String::new();
        if buf.read_line(&mut line).is_err() {
            // default to true if we can't read the file
            println!("should_export = true (failed to read the generated file)");
            return true;
        }

        if !line.starts_with("// rev: ") {
            // always rebuild if the first line isn't a valid revstring
            println!("should_export = true (invalid revstring)");
            return true;
        }

        let rev = line.trim_start_matches("// rev: ");
        if line.ends_with("-dirty") {
            // always rebuild if the generated file was from a dirty tree
            println!("should_export = true (dirty tree in generated file)");
            return true;
        }

        let current_rev = get_git_hash();
        if current_rev.ends_with("-dirty") {
            // always rebuild if the current tree is dirty
            println!("should_export = true (dirty current working tree)");
            return true;
        }

        println!("should_export = {} (generated {}, current {})", rev != current_rev, rev, current_rev);
        rev != current_rev
    }
}

fn get_git_hash() -> String {
    let output = Command::new("git")
        .arg("describe")
        .arg("--dirty")
        .arg("--always")
        .output();

    match output {
        Ok(output) => {
            if !output.status.success() {
                panic!("failed to get git hash: {}", String::from_utf8_lossy(&output.stderr));
            }

            let hash = String::from_utf8_lossy(&output.stdout);
            let hash = hash.trim();
            hash.to_owned()
        }
        Err(err) => {
            panic!("error while executing `git`: {}", err);
        }
    }
}

fn check_dotnet() {
    println!("checking .NET version");

    let output = Command::new("dotnet")
        .arg("--version")
        .output();

    match output {
        Ok(output) => {
            let version = String::from_utf8_lossy(&output.stdout);
            let version = version.trim();
            let version = version.split_whitespace().next().unwrap();
            let version = version.split('.').next().unwrap();
            let version = version.parse::<u32>().unwrap();
            if version < 7 {
                panic!(".NET 7 or later is required (found {}). Please install .NET 7 or later and try again.", version);
            }

            println!("found .NET {}", version);
        }
        Err(err) if err.kind() == io::ErrorKind::NotFound => {
            panic!("`dotnet` not found in path. Please install .NET 7 or later and try again.");
        }
        Err(err) => {
            panic!("error while executing `dotnet`: {}", err);
        }
    };
}

fn build_exporter() {
    println!("building exporter");

    let current_dir = env::var("CARGO_MANIFEST_DIR").ok().unwrap();
    let current_dir = Path::new(&current_dir);

    let output = Command::new("dotnet")
        .arg("build")
        .arg("--configuration")
        .arg("Release")
        .arg("exporter")
        .current_dir(current_dir.join(".."))
        .output();

    match output {
        Ok(output) => {
            if !output.status.success() {
                panic!("failed to build exporter: {}", String::from_utf8_lossy(&output.stderr));
            }
        }
        Err(err) => {
            panic!("error while executing `dotnet`: {}", err);
        }
    };
}

fn run_exporter() {
    println!("running exporter");

    let current_dir = env::var("CARGO_MANIFEST_DIR").ok().unwrap();
    let current_dir = Path::new(&current_dir);

    let exporter_dir = current_dir.join("../exporter/bin/Release/net7.0");
    let exporter_exe = exporter_dir.join("RustExporter.exe");

    let output = Command::new(exporter_exe)
        .current_dir(exporter_dir)
        .output();
    
    match output {
        Ok(output) => {
            if !output.status.success() {
                panic!("failed to run exporter: {}", String::from_utf8_lossy(&output.stderr));
            }
        }
        Err(err) => {
            panic!("error while executing `dotnet`: {}", err);
        }
    };
}

fn main() {
    // using our own rerun logic to be more granular
    if !should_export() {
        return;
    }

    check_dotnet();
    build_exporter();
    run_exporter();
}
