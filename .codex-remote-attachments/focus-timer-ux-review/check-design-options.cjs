const { chromium } = require('C:/Users/mklas/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/playwright');
const path = require('path');
const { pathToFileURL } = require('url');

(async () => {
  const browser = await chromium.launch({ headless: true, executablePath: 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe' });
  const page = await browser.newPage({ viewport: { width: 780, height: 1100 }, colorScheme: 'light' });
  const failures = [];
  page.on('pageerror', error => failures.push(error.message));
  await page.goto(pathToFileURL(path.join(__dirname, 'design-options-qa.html')).href, { waitUntil: 'networkidle' });
  const frame = page.frameLocator('iframe');
  await frame.locator('#ft-preview-glass-light').waitFor();
  const frameHandle = await page.locator('iframe').elementHandle();
  const innerFrame = await frameHandle.contentFrame();
  const categories = ['glass', 'layout', 'type', 'nav', 'controls'];
  const audit = [];
  for (const width of [780, 380, 340]) {
    await page.setViewportSize({ width, height: 1300 });
    for (const category of categories) {
      await frame.locator('#ft-' + category + '-tab').click();
      const panel = frame.locator('#ft-' + category + '-panel');
      const next = panel.locator('.viz-carousel-next');
      for (let index = 0; index < 3; index++) {
        const result = await innerFrame.evaluate(category => {
          const panel = document.getElementById('ft-' + category + '-panel');
          const variant = [...panel.querySelectorAll('[data-variant]')].find(item => !item.hidden);
          const product = variant.querySelector('.ft-product-window');
          const stage = variant.querySelector('.ft-stage');
          const outside = [...product.querySelectorAll('*')].filter(element => {
            if (!element.getClientRects().length) return false;
            const box = element.getBoundingClientRect();
            const limit = product.getBoundingClientRect();
            return box.left < limit.left - 1 || box.right > limit.right + 1;
          }).map(element => element.className?.baseVal ?? element.className);
          return { category, variant: variant.dataset.variant, width: window.innerWidth, bodyOverflow: document.documentElement.scrollWidth > window.innerWidth + 1, productOverflow: product.scrollWidth > product.clientWidth + 2, stageHeight: stage.clientHeight, stageOverflow: stage.scrollHeight > stage.clientHeight + 2, outside };
        }, category);
        audit.push(result);
        if (result.bodyOverflow || result.productOverflow || result.stageOverflow || result.outside.length) failures.push(JSON.stringify(result));
        await next.click();
      }
    }
  }
  const comparisonHeights = [];
  for (const width of [748, 348, 308]) for (const category of categories) {
    const heights = audit.filter(item => item.width === width && item.category === category).map(item => item.stageHeight);
    comparisonHeights.push({ width, category, heights });
    if (heights.length !== 3 || Math.max(...heights) - Math.min(...heights) > 1) failures.push('Unsteady comparison height: ' + JSON.stringify({ width, category, heights }));
  }
  await page.setViewportSize({ width: 780, height: 1050 });
  await frame.locator('#ft-controls-tab').click();
  const panel = frame.locator('#ft-controls-panel');
  await panel.locator('[data-choose="controls"][data-option="Soft rounded"]').click();
  if (!await panel.locator('[data-choice-label="controls"]').textContent().then(text => text.includes('Soft rounded'))) failures.push('Choice button failed');
  await panel.locator('.ft-range:visible').fill('72');
  if ((await panel.locator('output:visible').textContent()) !== '72%') failures.push('Slider output failed');
  await panel.locator('[data-page-link="logging"]:visible').click();
  await panel.locator('h2:visible').filter({ hasText: 'Logging' }).waitFor();
  await panel.locator('[data-page-link="appearance"]:visible').click();
  await panel.locator('.ft-save:visible').click();
  if ((await panel.locator('.ft-save-state:visible').textContent()) !== 'Preview saved') failures.push('Preview save action failed');
  await frame.locator('#ft-glass-tab').click();
  await page.screenshot({ path: path.join(__dirname, 'design-options-light.png'), fullPage: true });
  await page.emulateMedia({ colorScheme: 'dark' });
  await panel.locator('button').count();
  await frame.locator('#ft-layout-tab').click();
  await frame.locator('#ft-layout-panel .viz-carousel-next').click();
  await page.screenshot({ path: path.join(__dirname, 'design-options-dark.png'), fullPage: true });
  const fontStatus = await innerFrame.evaluate(async () => { await document.fonts.ready; return { inter: document.fonts.check('14px Inter'), manrope: document.fonts.check('14px Manrope'), loaded: [...document.fonts].filter(font => font.status === 'loaded').map(font => font.family) }; });
  console.log(JSON.stringify({ checks: audit.length, fontStatus, failures, comparisonHeights }, null, 2));
  await browser.close();
  if (failures.length) process.exitCode = 1;
})().catch(error => { console.error(error); process.exit(1); });
