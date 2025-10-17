using System;
using System.IO;
using Aog.Abstractions.Contracts;
using Google.Protobuf;

var bytes = new byte[GrpcContractRegistry.DescriptorSet.CalculateSize()];
GrpcContractRegistry.DescriptorSet.WriteTo(new Google.Protobuf.CodedOutputStream(bytes));
var base64 = Convert.ToBase64String(bytes);
File.WriteAllText("../../tests/Aog.Abstractions.Tests/Baselines/aog-contracts.desc.base64", base64);