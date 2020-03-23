﻿using System;
using System.Threading.Tasks;
using Compliance.Notifications.Data;
using LanguageExt;
using NUnit.Framework;
using Pri.LongPath;

namespace Compliance.Notifications.Common.Tests
{
    [TestFixture(Category = TestCategory.UnitTests)]
    public class FTests
    {
        [Test]
        [Category(TestCategory.UnitTests)]
        [TestCase("c:\\temp", "c:\\temp\\")]
        [TestCase("\\\\temp\\temp", "\\\\temp\\temp\\")]
        public void AppendDirectorySeparatorCharTest(string input, string expected)
        {
            var actual = input.AppendDirectorySeparatorChar();
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void AppendDirectorySeparatorCharTest_NullInput_Exception()
        {
            Assert.Throws<ArgumentException>(() =>
                {
                    var actual = F.AppendDirectorySeparatorChar(null);
                }
            );
        }

        [Test]
        public void AppendDirectorySeparatorCharTest_EmptyInput_Exception()
        {
            Assert.Throws<ArgumentException>(() =>
                {
                    var actual = "   ".AppendDirectorySeparatorChar();
                }
            );
        }

        [Test]
        public void GetFreeDiskSpaceTest()
        {
            F.GetFreeDiskSpaceInGigaBytes("c:\\").Match<decimal>(size =>
            {
                Assert.IsTrue(size > 0M);
                Assert.IsTrue(size < 10000M);
                return size;
            }, exception =>
             {
                 Assert.Fail(exception.ToString());
                 return 0;
             });
        }

        [Test()]
        [TestCase(@"test.txt", "somename1", @"test.somename1.txt")]
        public void AppendToFileNameTest(string fileName, string name, string expected)
        {
            var actual = F.AppendNameToFileName(fileName, name);
            Assert.AreEqual(expected, actual);
        }

        public class TestData : Record<TestData>
        {
            public TestData(Some<string> name, Some<string> description, UDecimal someNumber)
            {
                Description = description;
                SomeNumber = someNumber;
                Name = name;
            }

            public string Name { get; }

            public string Description { get; }

            public UDecimal SomeNumber { get; }
        }

        [Test]
        public async Task SaveAndLoadComplianceItemResultTest()
        {
            var testData = new TestData("A Name", "A description", 81.3452m);
            Some<string> fileName = $@"c:\temp\{typeof(TestData).Name}.json";
            var result = await F.SaveComplianceItemResult<TestData>(testData, fileName);
            result.Match<Unit>(unit =>
            {
                Assert.IsTrue(true);
                return Unit.Default;
            }, exception =>
            {
                Assert.Fail();
                return Unit.Default;
            });
            var loadedTestData = await F.LoadComplianceItemResult<TestData>(fileName);
            var ignore = loadedTestData.Match<TestData>(
                data =>
                        {
                            Assert.AreEqual("A Name", data.Name);
                            Assert.AreEqual("A description", data.Description);
                            Assert.AreEqual(new UDecimal(81.3452m), data.SomeNumber);
                            return data;
                        },
                exception =>
                        {
                            Assert.Fail(exception.Message);
                            throw exception;
                        });
        }

        [Test()]
        public void TryGetFilesTest()
        {
            var actual = F.TryGetFiles(new DirectoryInfo(@"c:\temp\UserTemp\msdtadmin"), "*.*");
            var files = actual.Try().Match<FileInfo[]>(infos =>
                {
                    Assert.IsTrue(false, "Not expected.");
                    return infos;
                }, exception =>
                {
                    Assert.True(true);
                    return new FileInfo[] { };
                });
        }

        [Test()]
        public async Task GetFolderSizeTest()
        {
            var actual = await F.GetFolderSize(@"c:\temp");
            var actualSize = actual.Match<UDecimal>(size =>
            {
                Assert.IsTrue(true);
                return size;
            }, exception =>
            {
                Assert.False(true,"Not expected to fail");
                return 0M;
            });
            Assert.IsTrue(actualSize > 0);
        }
    }
}