const fs = require('node:fs');
exports.publish = async (_, { nextRelease }) => {
  if (process.env.ACT !== 'true') throw new Error('Local release requires act');
  fs.mkdirSync('artifacts', { recursive: true });
  fs.writeFileSync('artifacts/local-release.json', JSON.stringify(nextRelease, null, 2));
};
