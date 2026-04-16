const fs = require('fs');
const path = require('path');

const apiBaseUrl =
  process.env.services__webapi__https__0 ||
  process.env.services__webapi__http__0 ||
  '';

const content = `export const environment = {
  apiBaseUrl: '${apiBaseUrl}',
};
`;

const envDir = path.join(__dirname, '..', 'src', 'environments');
fs.writeFileSync(path.join(envDir, 'environment.ts'), content);
fs.writeFileSync(path.join(envDir, 'environment.development.ts'), content);

console.log(`API Base URL set to: ${apiBaseUrl || '(empty)'}`);
