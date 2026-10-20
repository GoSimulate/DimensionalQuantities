module.exports = require(process.env.ACT === 'true'
  ? './.github/testing/release.config.cjs'
  : './.github/release.config.cjs');
