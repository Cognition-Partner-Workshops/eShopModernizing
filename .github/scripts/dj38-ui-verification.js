const assert = require("assert");
const fs = require("fs");
const path = require("path");
const { chromium } = require("playwright");

(async () => {
    const artifactsDirectory = path.join(process.env.GITHUB_WORKSPACE, "artifacts");
    fs.mkdirSync(artifactsDirectory, { recursive: true });

    const browser = await chromium.launch({ channel: "chrome", headless: true });
    const beforePage = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
    await beforePage.goto("http://127.0.0.1:5101/Catalog/Index?pageSize=5", { waitUntil: "networkidle" });
    assert.strictEqual(await beforePage.locator(".esh-filter").count(), 0);
    await beforePage.screenshot({
        path: path.join(artifactsDirectory, "catalog-before.png"),
        fullPage: true
    });

    const afterPage = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
    await afterPage.goto("http://127.0.0.1:5102/Catalog/Index?pageSize=2", { waitUntil: "networkidle" });
    await afterPage.locator("#searchName").fill(".NET");
    await afterPage.locator("#brandId").selectOption("2");
    await afterPage.locator("#typeId").selectOption("2");
    await Promise.all([
        afterPage.waitForNavigation({ waitUntil: "networkidle" }),
        afterPage.getByRole("button", { name: "Filter" }).click()
    ]);

    let url = new URL(afterPage.url());
    assert.strictEqual(url.searchParams.get("searchName"), ".NET");
    assert.strictEqual(url.searchParams.get("brandId"), "2");
    assert.strictEqual(url.searchParams.get("typeId"), "2");
    assert.strictEqual(await afterPage.locator("#searchName").inputValue(), ".NET");
    assert.strictEqual(await afterPage.locator("#brandId").inputValue(), "2");
    assert.strictEqual(await afterPage.locator("#typeId").inputValue(), "2");
    assert.ok((await afterPage.locator(".esh-pager").innerText()).includes("of 3 products"));

    await Promise.all([
        afterPage.waitForNavigation({ waitUntil: "networkidle" }),
        afterPage.getByRole("link", { name: "Next" }).click()
    ]);

    url = new URL(afterPage.url());
    assert.strictEqual(url.searchParams.get("pageIndex"), "1");
    assert.strictEqual(url.searchParams.get("searchName"), ".NET");
    assert.strictEqual(url.searchParams.get("brandId"), "2");
    assert.strictEqual(url.searchParams.get("typeId"), "2");
    assert.strictEqual(await afterPage.locator("#searchName").inputValue(), ".NET");
    assert.strictEqual(await afterPage.locator("#brandId").inputValue(), "2");
    assert.strictEqual(await afterPage.locator("#typeId").inputValue(), "2");
    assert.ok((await afterPage.locator(".esh-pager").innerText()).includes("Page 2 - 2"));
    await afterPage.screenshot({
        path: path.join(artifactsDirectory, "catalog-after.png"),
        fullPage: true
    });

    fs.writeFileSync(
        path.join(artifactsDirectory, "verification.txt"),
        "Verified case-insensitive name, brand, and type filters and preserved all filter values across Next pagination.\n"
    );
    await browser.close();
})().catch(error => {
    console.error(error);
    process.exit(1);
});
