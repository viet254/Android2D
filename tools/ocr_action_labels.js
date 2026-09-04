const fs = require('fs');
const path = require('path');
const tesseractRoot = 'C:/Users/vh/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/tesseract.js';
const { createWorker, PSM } = require(tesseractRoot);

const workspace = 'D:/game/Android2D';
const labelsRoot = path.join(workspace, 'sprite_analysis', 'tesseract_labels');
const cachePath = path.join(workspace, 'sprite_analysis', 'tessdata');
const entries = JSON.parse(fs.readFileSync(path.join(labelsRoot, 'index.json'), 'utf8'));

(async () => {
  fs.mkdirSync(cachePath, { recursive: true });
  const worker = await createWorker('eng', 1, {
    cachePath,
    logger: message => {
      if (message.status === 'recognizing text' && Math.round(message.progress * 100) % 25 === 0) {
        process.stderr.write(`OCR ${Math.round(message.progress * 100)}%\r`);
      }
    },
  });
  await worker.setParameters({
    tessedit_pageseg_mode: PSM.SINGLE_LINE,
    tessedit_char_whitelist: 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_-',
    preserve_interword_spaces: '1',
  });

  const results = [];
  for (let i = 0; i < entries.length; i += 1) {
    const entry = entries[i];
    const result = await worker.recognize(entry.path);
    const text = result.data.text.trim().replace(/\s+/g, '');
    const record = { ...entry, text, confidence: result.data.confidence };
    results.push(record);
    console.log(`${entry.source}\t${String(entry.index).padStart(3, '0')}\t${text}\t${result.data.confidence.toFixed(1)}`);
  }
  await worker.terminate();
  fs.writeFileSync(
    path.join(workspace, 'sprite_analysis', 'tesseract_action_labels.json'),
    JSON.stringify(results, null, 2),
    'utf8',
  );
})();
