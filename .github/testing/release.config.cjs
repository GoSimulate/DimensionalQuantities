// Exercise the real release lifecycle; only external publishing is substituted.
if (process.env.ACT !== 'true') throw new Error('This configuration is only for act');
const production = require('../release.config.cjs');
module.exports = {
  ...production,
  plugins: production.plugins.map(([name, options]) => {
    if (name === '@semantic-release/exec') {
      return [name, { ...options, publishCmd: 'bash .github/testing/publish-local.sh' }];
    }
    if (name === '@semantic-release/github') return './.github/testing/local-release.cjs';
    return [name, options];
  }),
};
