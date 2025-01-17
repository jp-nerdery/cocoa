﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Threading.Tasks;
using Acr.UserDialogs;
using Covid19Radar.Services;
using Covid19Radar.Services.Logs;
using Covid19Radar.ViewModels;
using Moq;
using Prism.Navigation;
using Xamarin.Essentials;
using Xunit;

namespace Covid19Radar.UnitTests.ViewModels
{
    public class InqueryPageViewModelTests
    {
        private readonly MockRepository mockRepository;
        private readonly Mock<INavigationService> mockNavigationService;
        private readonly Mock<ILoggerService> mockLoggerService;
        private readonly Mock<ILogFileService> mockLogFileService;
        private readonly Mock<ILogPathService> mockLogPathService;
        private readonly Mock<IEssentialsService> mockEssentialsService;

        private readonly Mock<IUserDialogs> mockUserDialogs;

        public InqueryPageViewModelTests()
        {
            mockRepository = new MockRepository(MockBehavior.Default);
            mockNavigationService = mockRepository.Create<INavigationService>();
            mockLoggerService = mockRepository.Create<ILoggerService>();
            mockLogFileService = mockRepository.Create<ILogFileService>();
            mockLogPathService = mockRepository.Create<ILogPathService>();

            mockUserDialogs = mockRepository.Create<IUserDialogs>();
            UserDialogs.Instance = mockUserDialogs.Object;

            mockEssentialsService = mockRepository.Create<IEssentialsService>();
        }

        private InqueryPageViewModel CreateViewModel()
        {
            var vm = new InqueryPageViewModel(
                mockNavigationService.Object,
                mockLoggerService.Object,
                mockLogFileService.Object,
                mockLogPathService.Object,
                mockEssentialsService.Object
                );
            return vm;
        }

        [Fact]
        public void OnClickQuestionCommandTests()
        {
            var unitUnderTest = CreateViewModel();
            unitUnderTest.Initialize(new NavigationParameters());

            var actualCalls = 0;
            string actualUri = default;
            BrowserLaunchMode actualLaunchMode = default;
            unitUnderTest.BrowserOpenAsync = (uri, launchMode) =>
            {
                actualCalls++;
                actualUri = uri;
                actualLaunchMode = launchMode;
                return Task.CompletedTask;
            };

            unitUnderTest.OnClickQuestionCommand.Execute(null);

            Assert.Equal(1, actualCalls);
            Assert.Equal("https://www.mhlw.go.jp/stf/seisakunitsuite/bunya/kenkou_iryou/covid19_qa_kanrenkigyou_00009.html", actualUri);
            Assert.Equal(BrowserLaunchMode.SystemPreferred, actualLaunchMode);
        }

        [Fact]
        public void OnClickSendLogCommandTests()
        {
            mockLogFileService.Setup(x => x.CreateZipFile(It.IsAny<string>()))
                .Returns("dummyFile");

            var unitUnderTest = CreateViewModel();
            unitUnderTest.Initialize(new NavigationParameters());

            unitUnderTest.OnClickSendLogCommand.Execute(null);

            mockNavigationService.Verify(x => x.NavigateAsync("SendLogConfirmationPage", It.IsAny<INavigationParameters>()), Times.Once());
        }

        [Fact]
        public void OnClickSendLogCommand_CreateZipSuccess()
        {
            var testLogId = "test-log-id";
            mockLogFileService.Setup(x => x.CreateLogId()).Returns(testLogId);

            var testZipFileName = "test-zip-file-name";
            mockLogFileService.Setup(x => x.CreateZipFileName(testLogId)).Returns(testZipFileName);
            var testPublicZipFileName = "test-public-zip-file-name";
            mockLogFileService.Setup(x => x.CreateZipFile(testZipFileName)).Returns(testPublicZipFileName);

            var unitUnderTest = CreateViewModel();
            unitUnderTest.Initialize(new NavigationParameters());

            unitUnderTest.OnClickSendLogCommand.Execute(null);

            mockLogFileService.Verify(x => x.CreateLogId(), Times.Once());
            mockLogFileService.Verify(x => x.CreateZipFileName(testLogId), Times.Once());
            mockLogFileService.Verify(x => x.CreateZipFile(testZipFileName), Times.Once());

            mockUserDialogs.Verify(x => x.ShowLoading(It.IsAny<string>(), null), Times.Once());
            mockUserDialogs.Verify(x => x.HideLoading(), Times.Once());
            mockUserDialogs.Verify(x => x.AlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), null), Times.Never());

            mockNavigationService.Verify(x => x.NavigateAsync(It.IsAny<string>()), Times.Never());
        }

        [Fact]
        public void OnClickSendLogCommand_CreateZipFailure()
        {
            var testLogId = "test-log-id";
            mockLogFileService.Setup(x => x.CreateLogId()).Returns(testLogId);

            var testZipFileName = "test-zip-file-name";
            mockLogFileService.Setup(x => x.CreateZipFileName(testLogId)).Returns(testZipFileName);
            mockLogFileService.Setup(x => x.CreateZipFile(testZipFileName)).Returns<string>(null);

            var unitUnderTest = CreateViewModel();
            unitUnderTest.Initialize(new NavigationParameters());

            unitUnderTest.OnClickSendLogCommand.Execute(null);

            mockLogFileService.Verify(x => x.CreateLogId(), Times.Once());
            mockLogFileService.Verify(x => x.CreateZipFileName(testLogId), Times.Once());
            mockLogFileService.Verify(x => x.CreateZipFile(testZipFileName), Times.Once());

            mockUserDialogs.Verify(x => x.ShowLoading(It.IsAny<string>(), null), Times.Once());
            mockUserDialogs.Verify(x => x.HideLoading(), Times.Once());
            mockUserDialogs.Verify(x => x.AlertAsync(It.IsAny<string>(), It.IsAny<string>(), "OK", null), Times.Once());
        }

        [Fact]
        public void OnClickEmailCommandTests_Success()
        {
            var unitUnderTest = CreateViewModel();
            unitUnderTest.Initialize(new NavigationParameters());

            var actualCalls = 0;
            string actualSubject = default;
            string actualBody = default;
            string[] actualTo = default;
            unitUnderTest.ComposeEmailAsync = (subject, body, to) =>
            {
                actualCalls++;
                actualSubject = subject;
                actualBody = body;
                actualTo = to;
                return Task.CompletedTask;
            };

            unitUnderTest.OnClickEmailCommand.Execute(null);

            Assert.Equal(1, actualCalls);
            Assert.NotEmpty(actualSubject);
            Assert.NotEmpty(actualBody);
            Assert.Single(actualTo);
            Assert.NotEmpty(actualTo[0]);
        }

        [Fact]
        public void OnClickEmailCommandTests_Exception()
        {
            var unitUnderTest = CreateViewModel();
            unitUnderTest.Initialize(new NavigationParameters());

            var actualCalls = 0;
            unitUnderTest.ComposeEmailAsync = (subject, body, to) =>
            {
                actualCalls++;
                throw new Exception();
            };

            unitUnderTest.OnClickEmailCommand.Execute(null);

            Assert.Equal(1, actualCalls);
        }

        [Fact]
        public void OnClickAboutAppCommandTests()
        {
            var unitUnderTest = CreateViewModel();
            unitUnderTest.Initialize(new NavigationParameters());

            var actualCalls = 0;
            string actualUri = default;
            BrowserLaunchMode actualLaunchMode = default;
            unitUnderTest.BrowserOpenAsync = (uri, launchMode) =>
            {
                actualCalls++;
                actualUri = uri;
                actualLaunchMode = launchMode;
                return Task.CompletedTask;
            };

            unitUnderTest.OnClickAboutAppCommand.Execute(null);

            Assert.Equal(1, actualCalls);
            Assert.Equal("https://www.mhlw.go.jp/stf/seisakunitsuite/bunya/cocoa_00138.html", actualUri);
            Assert.Equal(BrowserLaunchMode.SystemPreferred, actualLaunchMode);
        }
    }
}
