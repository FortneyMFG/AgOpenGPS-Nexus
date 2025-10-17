using System;
using System.IO;
using Aog.Abstractions.Contracts;

var bytes = GrpcContractRegistry.DescriptorSet.ToByteArray();
var base64 = Convert.ToBase64String(bytes);
File.WriteAllText("tests/Aog.Abstractions.Tests/Baselines/aog-contracts.desc.base64", base64);