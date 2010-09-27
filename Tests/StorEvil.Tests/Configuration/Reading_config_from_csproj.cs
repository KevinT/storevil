﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using NUnit.Framework;
using StorEvil.Assertions;

namespace StorEvil.Configuration
{
    [TestFixture]
    public class Reading_config_from_csproj
    {
        private CsProjParser _parser;

        [SetUp]
        public void SetUpContext()
        {
            _parser = new CsProjParser(_exampleCsProj);
        }

        [Test]
        public void Finds_output_assembly()
        {            
            _parser.GetAssemblyLocation().ToLower().ShouldEqual("bin\\debug\\Bowling.dll".ToLower());
        }

        [Test]
        public void Finds_potential_stories()
        {
            var files = _parser.GetFilesWithTypeNone();
            files.ElementsShouldEqual(
                "Stories\\ScoreCalculation.feature",
                "Stories\\ScoreCalculationAlternatives.feature"
                );
        }

        private const string _exampleCsProj = 
@"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <ProductVersion>9.0.30729</ProductVersion>
    <SchemaVersion>2.0</SchemaVersion>
    <ProjectGuid>{43C70F9D-3747-43E3-A76B-8A8A0C2F15CC}</ProjectGuid>
    <OutputType>Library</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <RootNamespace>Bowling</RootNamespace>
    <AssemblyName>Bowling</AssemblyName>
    <TargetFrameworkVersion>v3.5</TargetFrameworkVersion>
    <FileAlignment>512</FileAlignment>
    <FileUpgradeFlags>
    </FileUpgradeFlags>
    <OldToolsVersion>3.5</OldToolsVersion>
    <UpgradeBackupLocation />
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>bin\Debug\</OutputPath>
    <DefineConstants>DEBUG;TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "">
    <DebugType>pdbonly</DebugType>
    <Optimize>true</Optimize>
    <OutputPath>bin\Release\</OutputPath>
    <DefineConstants>TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""nunit.framework, Version=2.4.8.0, Culture=neutral, PublicKeyToken=96d09a1eb7f44a77, processorArchitecture=MSIL"">
      <SpecificVersion>False</SpecificVersion>
      <HintPath>..\..\Lib\Test\nunit.framework.dll</HintPath>
    </Reference>
    <Reference Include=""System"" />
    <Reference Include=""System.Core"">
      <RequiredTargetFramework>3.5</RequiredTargetFramework>
    </Reference>
    <Reference Include=""System.Xml.Linq"">
      <RequiredTargetFramework>3.5</RequiredTargetFramework>
    </Reference>
    <Reference Include=""System.Data.DataSetExtensions"">
      <RequiredTargetFramework>3.5</RequiredTargetFramework>
    </Reference>
    <Reference Include=""System.Data"" />
    <Reference Include=""System.Xml"" />
  </ItemGroup>
  <ItemGroup>
    <Compile Include=""Context\BowlingContext.cs"" />
    <Compile Include=""Model\BowlingGame.cs"" />
    <Compile Include=""Properties\AssemblyInfo.cs"" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include=""..\..\Core\StorEvil\StorEvil.Core.csproj"">
      <Project>{B86235CA-045F-4222-997C-A65E966C6F9B}</Project>
      <Name>StorEvil.Core</Name>
    </ProjectReference>
  </ItemGroup>
  <ItemGroup>
    <None Include=""Stories\ScoreCalculation.feature"" />
    <None Include=""Stories\ScoreCalculationAlternatives.feature"" />
  </ItemGroup>
  <ItemGroup>
    <Content Include=""Stories\LICENSE.txt"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
  <!-- To modify your build process, add your task inside one of the targets below and uncomment it. 
       Other similar extension points exist, see Microsoft.Common.targets.
  <Target Name=""BeforeBuild"">
  </Target>
  <Target Name=""AfterBuild"">
  </Target>
  -->
</Project>";
    }

    public class XmlDocumentWrapper
    {
        private XmlDocument _doc;
        private XmlNamespaceManager _mgr;

        public XmlDocumentWrapper(XmlDocument doc)
        {
            _doc = doc;
            _mgr = new XmlNamespaceManager(_doc.NameTable);
           
        }

        public XmlElement SelectElement(string xpath)
        {
            return _doc.SelectSingleNode(xpath, _mgr) as XmlElement;
        }

        public IEnumerable<XmlElement> SelectElements(string xpath)
        {
            foreach (var node in _doc.SelectNodes(xpath, _mgr))
            {
                if (node is XmlElement)
                    yield return (XmlElement)node;
            }
        }

        public void AliasNamespace(string namespaceAlias, string namespaceUrl)
        {
            _mgr.AddNamespace(namespaceAlias, namespaceUrl);
        }
    }

    public class CsProjParser
    {
        private XmlDocumentWrapper _doc;
        
        public CsProjParser(string exampleCsProj)
        {
            var doc = new XmlDocument();
            doc.LoadXml(exampleCsProj);
            _doc = new XmlDocumentWrapper(doc);
            _doc.AliasNamespace("x", "http://schemas.microsoft.com/developer/msbuild/2003");         
        }

        public string GetAssemblyLocation()
        {
            var assemblyName = _doc.SelectElement("//x:PropertyGroup/x:AssemblyName").InnerText;
            var path = _doc.SelectElement("//x:PropertyGroup/x:OutputPath").InnerText;
            return Path.Combine(path, assemblyName + ".dll");
        }

        public IEnumerable<string> GetFilesWithTypeNone()
        {
            var nodes = _doc.SelectElements("//x:ItemGroup/x:None");

            foreach (var node in nodes)
            {
                yield return node.GetAttribute("Include");
            }           
        }
    }
}