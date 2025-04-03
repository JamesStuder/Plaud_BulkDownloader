using System;
using System.Collections.Generic;
using System.Linq;
using API.Plaud.NET.Models;
using Plaud.ConsoleApp.BulkDownloader.Models;
using Xunit;

namespace Plaud.ConsoleApp.BulkDownloader.Tests
{
    public class FilterRecordingsByDateTests
    {
        [Fact]
        public void FilterRecordingsByDate_NullRecordings_ReturnsEmptyList()
        {
            // Arrange
            var userInput = new UserInput { StartDate = "2023-01-01" };
            List<DataFileList> recordings = null;

            // Act
            var result = Program.FilterRecordingsByDate(userInput, recordings);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void FilterRecordingsByDate_EmptyRecordings_ReturnsEmptyList()
        {
            // Arrange
            var userInput = new UserInput { StartDate = "2023-01-01" };
            var recordings = new List<DataFileList>();

            // Act
            var result = Program.FilterRecordingsByDate(userInput, recordings);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void FilterRecordingsByDate_NullUserInput_ReturnsAllRecordings()
        {
            // Arrange
            UserInput userInput = null;
            var recordings = CreateSampleRecordings();

            // Act
            var result = Program.FilterRecordingsByDate(userInput, recordings);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(recordings.Count, result.Count);
        }

        [Fact]
        public void FilterRecordingsByDate_EmptyStartDate_ReturnsAllRecordings()
        {
            // Arrange
            var userInput = new UserInput { StartDate = "" };
            var recordings = CreateSampleRecordings();

            // Act
            var result = Program.FilterRecordingsByDate(userInput, recordings);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(recordings.Count, result.Count);
        }

        [Fact]
        public void FilterRecordingsByDate_InvalidDateFormat_ReturnsAllRecordings()
        {
            // Arrange
            var userInput = new UserInput { StartDate = "invalid-date" };
            var recordings = CreateSampleRecordings();

            // Act
            var result = Program.FilterRecordingsByDate(userInput, recordings);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(recordings.Count, result.Count);
        }

        [Fact]
        public void FilterRecordingsByDate_ValidStartDate_FiltersCorrectly()
        {
            // Arrange
            var userInput = new UserInput { StartDate = "2023-06-15" };
            var recordings = CreateSampleRecordings();

            // Act
            var result = Program.FilterRecordingsByDate(userInput, recordings);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count); // Only recordings from June 15, 2023 and later
            Assert.All(result, r => Assert.True(DateTimeOffset.FromUnixTimeMilliseconds(r.StartTime).DateTime >= DateTime.Parse("2023-06-15")));
        }

        [Fact]
        public void FilterRecordingsByDate_StartDateAfterAllRecordings_ReturnsEmptyList()
        {
            // Arrange
            var userInput = new UserInput { StartDate = "2024-01-01" };
            var recordings = CreateSampleRecordings();

            // Act
            var result = Program.FilterRecordingsByDate(userInput, recordings);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        private List<DataFileList> CreateSampleRecordings()
        {
            return new List<DataFileList>
            {
                new DataFileList
                {
                    Id = "1",
                    Filename = "Recording1",
                    StartTime = DateTimeOffset.Parse("2023-01-15T10:00:00Z").ToUnixTimeMilliseconds()
                },
                new DataFileList
                {
                    Id = "2",
                    Filename = "Recording2",
                    StartTime = DateTimeOffset.Parse("2023-03-20T14:30:00Z").ToUnixTimeMilliseconds()
                },
                new DataFileList
                {
                    Id = "3",
                    Filename = "Recording3",
                    StartTime = DateTimeOffset.Parse("2023-06-15T09:15:00Z").ToUnixTimeMilliseconds()
                },
                new DataFileList
                {
                    Id = "4",
                    Filename = "Recording4",
                    StartTime = DateTimeOffset.Parse("2023-12-31T23:59:59Z").ToUnixTimeMilliseconds()
                }
            };
        }
    }
} 