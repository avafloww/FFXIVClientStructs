using RustExporter;

File.WriteAllText("../../../../rust/src/generated.rs", Exporter.Instance.Export());