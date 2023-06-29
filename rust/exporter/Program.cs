using RustExporter;

File.WriteAllText("../../../../generated/src/generated.rs", Exporter.Instance.Export());
