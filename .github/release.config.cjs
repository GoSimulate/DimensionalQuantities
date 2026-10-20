module.exports = {
  branches: ['main'],
  tagFormat: 'v${version}',
  plugins: [
    ['@semantic-release/commit-analyzer', { preset: 'conventionalcommits' }],
    ['@semantic-release/release-notes-generator', { preset: 'conventionalcommits' }],
    ['@semantic-release/exec', {
      prepareCmd: 'bash .github/scripts/pack.sh ${nextRelease.version}',
      publishCmd: 'bash .github/scripts/publish.sh',
    }],
    ['@semantic-release/github', {
      assets: [{ path: 'artifacts/packages/*.nupkg', label: 'NuGet package' }],
      successComment: false,
      failComment: false,
      releasedLabels: false,
    }],
  ],
};
