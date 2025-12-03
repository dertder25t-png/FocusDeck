
import time
from playwright.sync_api import sync_playwright

def verify_frontend():
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        context = browser.new_context(ignore_https_errors=True)
        page = context.new_page()

        try:
            page.goto("http://localhost:5173", timeout=10000)
            print("Navigated to localhost:5173")
            time.sleep(5)
            page.screenshot(path="verification.png")
            print("Screenshot taken.")
        except Exception as e:
            print(f"Navigation failed: {e}")

        browser.close()

if __name__ == "__main__":
    verify_frontend()
