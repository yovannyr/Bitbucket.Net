﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Bitbucket.Net.Common;
using Bitbucket.Net.Models;
using Xunit;

namespace Bitbucket.Net.Tests
{
    public class BitbucketClientShould
    {
        private readonly BitbucketClient _client;

        public BitbucketClientShould()
        {
            _client = new BitbucketClient("", "", "");
        }

        [Fact]
        public async Task GetProjectsAsync()
        {
            var results = await _client.GetProjectsAsync().ConfigureAwait(false);
            Assert.True(results.Any());
        }

        [Theory]
        [InlineData("Tools", 1)]
        public async Task GetRepositoriesAsync(string projectKey, int maxPages)
        {
            var results = await _client.GetRepositoriesAsync(projectKey, maxPages: maxPages).ConfigureAwait(false);
            Assert.True(results.Any());
        }

        [Theory]
        [InlineData("Tools", "Test")]
        public async Task GetRepositoryGroupPermissionsAsync(string projectKey, string repositorySlug)
        {
            var results = await _client.GetRepositoryGroupPermissionsAsync(projectKey, repositorySlug).ConfigureAwait(false);
            Assert.True(results.Any());
        }

        [Theory]
        [InlineData("Tools", "Test")]
        public async Task GetRepositoryUserPermissionsAsync(string projectKey, string repositorySlug)
        {
            var results = await _client.GetRepositoryUserPermissionsAsync(projectKey, repositorySlug).ConfigureAwait(false);
            Assert.False(results.Any());
        }

        [Theory]
        [InlineData("Tools", "Test")]
        public async Task GetBranchesAsync(string projectKey, string repositorySlug)
        {
            var results = await _client.GetBranchesAsync(projectKey, repositorySlug, maxPages: 1).ConfigureAwait(false);
            Assert.True(results.Any());
        }

        [Theory]
        [InlineData("Tools", "Test", 3)]
        public async Task GetBranchesToDeleteAsync(string projectKey, string repositorySlug, int daysOlderThanToday)
        {
            var results = await _client.GetBranchesAsync(projectKey, repositorySlug, details: true).ConfigureAwait(false);
            var list = results.ToList();
            Assert.True(list.Any());

            var deleteStates = new[] { PullRequestState.Merged, PullRequestState.Declined };
            var branchesToDelete = list.Where(branch => 
                !branch.IsDefault
                && deleteStates.Any(state => state == branch.BranchMetadata?.OutgoingPullRequest?.PullRequest?.State)
                && branch.BranchMetadata?.OutgoingPullRequest?.PullRequest?.UpdatedDate.FromUnixTimeSeconds() < DateTimeOffset.UtcNow.Date.AddDays(-daysOlderThanToday)
                && branch.BranchMetadata?.AheadBehind?.Ahead == 0);

            Assert.NotNull(branchesToDelete);
        }

        [Theory]
        [InlineData("Tools", "Test", PullRequestState.All)]
        [InlineData("Tools", "Test", PullRequestState.Merged)]
        public async Task GetPullRequestsAsync(string projectKey, string repositorySlug, PullRequestState state)
        {
            var results = await _client.GetPullRequestsAsync(projectKey, repositorySlug, state: state, maxPages: 1).ConfigureAwait(false);
            Assert.True(results.Any());
        }

        [Theory]
        [InlineData("Tools", "Test", PullRequestState.All)]
        public async Task GetPullRequestAsync(string projectKey, string repositorySlug, PullRequestState state)
        {
            var results = await _client.GetPullRequestsAsync(projectKey, repositorySlug, state: state, maxPages: 1).ConfigureAwait(false);
            var list = results.ToList();
            Assert.True(list.Any());
            int id = list.First().Id;

            var result = await _client.GetPullRequestAsync(projectKey, repositorySlug, id).ConfigureAwait(false);
            Assert.NotNull(result);
        }

        [Theory]
        [InlineData("Tools", "Test")]
        public async Task CreateAndDeletePullRequestAsync(string projectKey, string repositorySlug)
        {
            var result = await _client.CreatePullRequestAsync(projectKey, repositorySlug, new PullRequestInfo
            {
                Title = "Test Pull Request",
                Description = "This is a test pull request",
                State = PullRequestState.Open,
                Open = true,
                Closed = false,
                FromRef = new FromToRef
                {
                    Id = "refs/heads/feature-test",
                    Repository = new RepositoryRef
                    {
                        Name = null,
                        Slug = repositorySlug,
                        Project = new ProjectRef { Key = projectKey }
                    }
                },
                ToRef = new FromToRef
                {
                    Id = "refs/heads/master",
                    Repository = new RepositoryRef
                    {
                        Name = null,
                        Slug = repositorySlug,
                        Project = new ProjectRef { Key = projectKey }
                    }
                },
                Locked = false
            }).ConfigureAwait(false);

            int id = result.Id;
            var pullRequest = await _client.GetPullRequestAsync(projectKey, repositorySlug, id).ConfigureAwait(false);
            Assert.NotNull(pullRequest);

            await _client.DeletePullRequest(projectKey, repositorySlug, pullRequest).ConfigureAwait(false);
        }
    }
}
