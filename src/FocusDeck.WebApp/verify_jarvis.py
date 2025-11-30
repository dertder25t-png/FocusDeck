from playwright.sync_api import sync_playwright

def verify_jarvis_chat():
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        # Use a desktop viewport
        context = browser.new_context(viewport={'width': 1920, 'height': 1080})
        page = context.new_page()

        # Capture console logs
        page.on("console", lambda msg: print(f"BROWSER CONSOLE: {msg.type}: {msg.text}"))
        page.on("pageerror", lambda err: print(f"BROWSER ERROR: {err}"))

        print("Navigating to app...")
        try:
            page.goto("http://localhost:5173", timeout=60000)
            print("Navigation complete.")
        except Exception as e:
            print(f"Navigation failed: {e}")
            raise e

        # Wait for app to load - check for taskbar start button
        print("Waiting for 'Start' button...")
        try:
            page.wait_for_selector("text=Start", timeout=60000)
            print("'Start' button found.")
        except Exception as e:
            page.screenshot(path="/home/jules/verification/jarvis_fail_load.png")
            print(f"Failed to find Start button: {e}")
            raise e

        # 1. Open Jarvis App
        # Open Start Menu
        page.click("text=Start")

        # Click Jarvis button inside Start Menu sidebar
        # The button contains "Jarvis" text
        page.locator("#start-menu button:has-text('Jarvis')").click()

        # Wait for Jarvis window to appear
        page.wait_for_selector("#win-jarvis")

        # 2. Verify Chat Interface
        # Check for system message
        expect_system_msg = page.locator("#win-jarvis .markdown-body").first
        expect_system_msg.wait_for(state='visible')

        # 3. Test Context Switching
        # Find context sidebar buttons - text "Code Companion"
        code_context_btn = page.locator("text=Code Companion")
        code_context_btn.click()

        # Verify the placeholder text in textarea changes
        textarea = page.locator("#win-jarvis textarea")
        placeholder = textarea.get_attribute("placeholder")
        assert "Code Companion" in placeholder

        # 4. Test Sending a Message
        textarea.fill("Write a hello world function")

        # Click send button (fa-paper-plane)
        page.click("#win-jarvis button .fa-paper-plane")

        # Wait for user message to appear (bg-accent-blue)
        page.wait_for_selector(".bg-accent-blue")

        # Wait for response (simulated delay)
        # We need to wait for the SECOND .markdown-body or look for pre code
        # Wait for the response to be rendered
        response_code = page.locator("#win-jarvis pre code")
        response_code.wait_for(state='visible', timeout=10000)

        # Take screenshot
        page.screenshot(path="/home/jules/verification/jarvis_chat.png")
        print("Jarvis chat verification successful.")

        browser.close()

if __name__ == "__main__":
    verify_jarvis_chat()
