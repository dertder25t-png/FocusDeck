
import time
from playwright.sync_api import sync_playwright

def diagnose_frontend():
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        context = browser.new_context(ignore_https_errors=True)
        page = context.new_page()

        # Capture console logs
        page.on("console", lambda msg: print(f"CONSOLE {msg.type}: {msg.text}"))
        page.on("pageerror", lambda exc: print(f"PAGE ERROR: {exc}"))

        try:
            print("Navigating to http://localhost:5173...")
            page.goto("http://localhost:5173", timeout=10000)
            time.sleep(5)
            page.screenshot(path="diagnosis.png")
            print("Screenshot taken.")
        except Exception as e:
            print(f"Navigation failed: {e}")

        browser.close()

if __name__ == "__main__":
    diagnose_frontend()
