# Playwright base image ships with Chromium pre-installed.
# Pin the same version as the playwright package in package.json.
FROM mcr.microsoft.com/playwright:v1.44.0-jammy

WORKDIR /app

COPY package.json ./

# PLAYWRIGHT_SKIP_BROWSER_DOWNLOAD prevents re-downloading Chromium;
# the base image already has it at the expected cache path.
ENV PLAYWRIGHT_SKIP_BROWSER_DOWNLOAD=1
RUN npm install

COPY src/ ./src/

ENV PORT=3000
EXPOSE 3000

CMD ["node", "src/index.js"]
