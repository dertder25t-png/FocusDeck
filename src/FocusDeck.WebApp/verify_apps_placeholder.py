from playwright.sync_api import sync_playwright

def verify_apps():
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        context = browser.new_context(viewport={'width': 1920, 'height': 1080})
        page = context.new_page()

        print("Navigating to app...")
        page.goto("http://localhost:5173")
        page.wait_for_selector("text=Start", timeout=60000)

        # 1. Verify Whiteboard
        print("Testing Whiteboard...")
        # Open Start Menu
        start_btn = page.locator("button:has-text('Start')")
        start_btn.click()

        # Wait for start menu to have opacity-100
        print("Waiting for Start Menu to open...")
        try:
            page.wait_for_selector("#start-menu.opacity-100", timeout=5000)
        except Exception as e:
            print("Start menu did not open (opacity-100 not found).")
            page.screenshot(path="/home/jules/verification/debug_start_menu.png")
            # Print class of start menu
            print(f"Start menu classes: {page.get_attribute('#start-menu', 'class')}")
            raise e

        # Click "Canvas" button in Start Menu
        print("Clicking Canvas...")
        canvas_btn = page.locator("#start-menu button:has-text('Canvas')")
        # Ensure it's visible
        if not canvas_btn.is_visible():
             print("Canvas button is not visible!")
             page.screenshot(path="/home/jules/verification/debug_canvas_btn.png")

        canvas_btn.click()

        # Wait for Whiteboard window
        print("Waiting for Whiteboard window...")
        page.wait_for_selector("#win-whiteboard", state='visible', timeout=10000)

        # Check for Canvas Stage (konvajs-content)
        page.wait_for_selector(".konvajs-content", state='visible')
        print("Whiteboard verified.")

        # Draw something (Optional but good)
        # canvas = page.locator(".konvajs-content canvas")
        # box = canvas.bounding_box()
        # page.mouse.move(box['x'] + 100, box['y'] + 100)
        # page.mouse.down()
        # page.mouse.move(box['x'] + 200, box['y'] + 200)
        # page.mouse.up()

        page.screenshot(path="/home/jules/verification/whiteboard.png")

        # 2. Verify Email
        print("Testing Email...")
        # Open Start Menu again
        start_btn.click()
        page.wait_for_selector("#start-menu.opacity-100", timeout=5000)

        # Click "Email"
        print("Clicking Email...")
        page.click("#start-menu button:has-text('Email')")

        # Wait for Email window
        print("Waiting for Email window...")
        page.wait_for_selector("#win-email", state='visible', timeout=10000)

        # Check for Inbox
        page.wait_for_selector("text=Inbox", state='visible')

        # Click "Compose"
        print("Clicking Compose...")
        page.click("text=Compose")

        # Check for Modal
        page.wait_for_selector("text=New Message", state='visible')

        # Fill form
        page.fill("input[placeholder='To']", "harry@hogwarts.edu")
        page.fill("input[placeholder='Subject']", "Quidditch Practice")
        page.fill("textarea", "Don't forget your broom!")

        # Send
        page.click("text=Send")

        # Verify modal closed and we are in Sent folder (active folder sent)
        print("Verifying sent...")
        page.wait_for_selector("h2:has-text('Sent')", state='visible')

        # Verify new email in list
        page.wait_for_selector("text=Quidditch Practice", state='visible')

        print("Email verified.")
        page.screenshot(path="/home/jules/verification/email.png")

        browser.close()

if __name__ == "__main__":
    verify_apps()
