using RustExporter;

File.WriteAllText("../../../../lib/src/generated.rs", Exporter.Instance.Export());