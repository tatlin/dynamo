﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Media.Imaging;
using DSCoreNodesUI.Input;
using Dynamo.Models;
using Dynamo.Tests;
using Dynamo.Wpf;
using Dynamo.Wpf.Controls;
using DynamoConversions;
using Dynamo.Nodes;
using NUnit.Framework;
using DynamoConverter = DSCoreNodesUI.DynamoConvert;

namespace DynamoCoreWpfTests
{
    [TestFixture]
    public class DynamoConverterTest : DSEvaluationViewModelUnitTest
    {
        public override void Setup()
        {
            // Add an assembly resolver to look in the nodes folder.
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            base.Setup();
        }

        public override void Cleanup()
        {
            base.Cleanup();
            AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
        }

        protected override void GetLibrariesToPreload(List<string> libraries)
        {
            libraries.Add("VMDataBridge.dll");
            libraries.Add("DynamoConversions.dll");
            libraries.Add("DynamoUnits.dll");
            base.GetLibrariesToPreload(libraries);
        }

        System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            // Look in the nodes folder
            string assemblyPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "nodes", new AssemblyName(args.Name).Name + ".dll");
            return File.Exists(assemblyPath) ? Assembly.LoadFrom(assemblyPath) : null;
        }

        [Test]
        public void CanConstructConverterNode()
        {
            var converter = new DynamoConverter();
            Assert.NotNull(converter);
        }

        [Test]
        public void ConverterDefaultValues()
        {
            var converter = new DynamoConverter();
            Assert.NotNull(converter);

            Assert.AreEqual(ConversionMetricUnit.Length, converter.SelectedMetricConversion);
            Assert.AreEqual(ConversionUnit.Meters, converter.SelectedFromConversion);
            Assert.AreEqual(ConversionUnit.Meters, converter.SelectedToConversion);
        }

        [Test]
        public void SetConverterValues()
        {
            var converter = new DynamoConverter() {SelectedMetricConversion =ConversionMetricUnit.Area};
            Assert.NotNull(converter);

            Assert.AreEqual(ConversionMetricUnit.Area, converter.SelectedMetricConversion);
            Assert.AreEqual(ConversionUnit.SquareMeter, converter.SelectedFromConversion);
            Assert.AreEqual(ConversionUnit.SquareMeter, converter.SelectedToConversion);
        }

        [Test]
        public void ConverterItemSourceCount()
        {
            var converter = new DynamoConverter() { SelectedMetricConversion = ConversionMetricUnit.Volume };
            Assert.NotNull(converter);

            Assert.AreEqual("8", ((dynamic)converter.SelectedFromConversionSource).Count.ToString());
            Assert.AreEqual("8", ((dynamic)converter.SelectedToConversionSource).Count.ToString());
        }

        [Test]
        public void ConvertSetConversionFromValue()
        {
            var converter = new DynamoConverter() { SelectedMetricConversion = ConversionMetricUnit.Length }; 
            Assert.NotNull(converter);

            Assert.AreEqual(ConversionMetricUnit.Length, converter.SelectedMetricConversion);
            Assert.AreEqual(ConversionUnit.Meters, converter.SelectedFromConversion);
            Assert.AreEqual(ConversionUnit.Meters, converter.SelectedToConversion);

            converter.SelectedFromConversion = ConversionUnit.Feet;
            Assert.AreEqual(ConversionUnit.Feet, converter.SelectedFromConversion);
            Assert.AreEqual(ConversionUnit.Meters, converter.SelectedToConversion);
        }

        [Test]
        public void ConvertSetConversionToValue()
        {
            var converter = new DynamoConverter() { SelectedMetricConversion = ConversionMetricUnit.Volume };
            Assert.NotNull(converter);
          
            Assert.AreEqual(ConversionMetricUnit.Volume, converter.SelectedMetricConversion);
            Assert.AreEqual(ConversionUnit.CubicMeters, converter.SelectedFromConversion);
            Assert.AreEqual(ConversionUnit.CubicMeters, converter.SelectedToConversion);

            converter.SelectedToConversion = ConversionUnit.CubicInches;
            Assert.AreEqual(ConversionUnit.CubicMeters, converter.SelectedFromConversion);
            Assert.AreEqual(ConversionUnit.CubicInches, converter.SelectedToConversion);
        }

        [Test]
        public void ConverterTestToggleState()
        {
            var converter = new DynamoConverter() { SelectedMetricConversion = ConversionMetricUnit.Length };
            Assert.NotNull(converter);

            Assert.AreEqual(ConversionMetricUnit.Length, converter.SelectedMetricConversion);
            Assert.AreEqual(ConversionUnit.Meters, converter.SelectedFromConversion);
            Assert.AreEqual(ConversionUnit.Meters, converter.SelectedToConversion);

            converter.SelectedFromConversion = ConversionUnit.Feet;
            Assert.AreEqual(ConversionUnit.Feet, converter.SelectedFromConversion);
            Assert.AreEqual(ConversionUnit.Meters, converter.SelectedToConversion);

            converter.ToggleDropdownValues();
            Assert.AreEqual(ConversionUnit.Meters, converter.SelectedFromConversion);
            Assert.AreEqual(ConversionUnit.Feet, converter.SelectedToConversion);
        }

        [Test]
        public void ConvertBetweenUnitsTestForForceReExecute()
        {
            var model = ViewModel.Model;
            string openPath = Path.Combine(TestDirectory, @"core\ConvertBetweenUnitsTest.dyn");
            RunModel(openPath);

            var node1 = model.CurrentWorkspace.NodeFromWorkspace("1371db60-371d-406b-a613-2f71ee43ccee");
            Assert.NotNull(node1);
            Assert.IsAssignableFrom(typeof(DoubleInput), node1);
            Assert.AreEqual("10", ((DoubleInput)node1).Value);

            /* Initial Conversion from Meters to MilliMeters */
            var converterNode =
                model.CurrentWorkspace.NodeFromWorkspace("069b3f3c-bc81-4c2a-b705-69a811cc43da") as DynamoConverter;
            Assert.NotNull(converterNode);
            Assert.AreEqual(ConversionUnit.Meters, converterNode.SelectedFromConversion);
            Assert.AreEqual(ConversionUnit.Millimeters, converterNode.SelectedToConversion);            
            AssertPreviewValue("45f1ee23-5d81-4233-975e-faf218203de5", 10000.0);

            /* Now convert from Meters to Decimeters */
            converterNode.SelectedFromConversion = ConversionUnit.Meters;
            converterNode.SelectedToConversion = ConversionUnit.Decimeters;
            Assert.AreEqual(ConversionUnit.Meters, converterNode.SelectedFromConversion);
            Assert.AreEqual(ConversionUnit.Decimeters, converterNode.SelectedToConversion);

            ViewModel.HomeSpace.Run();
            Thread.Sleep(500);

            AssertPreviewValue("45f1ee23-5d81-4233-975e-faf218203de5", 100.0);

            /* Again convert from Meters to MilliMeters */
            converterNode.SelectedToConversion = ConversionUnit.Millimeters;
            Assert.AreEqual(ConversionUnit.Meters, converterNode.SelectedFromConversion);
            Assert.AreEqual(ConversionUnit.Millimeters, converterNode.SelectedToConversion);

            ViewModel.HomeSpace.Run();
            Thread.Sleep(500);

            AssertPreviewValue("45f1ee23-5d81-4233-975e-faf218203de5", 10000.0);
        }
    }
}
