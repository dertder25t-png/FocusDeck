import { Card, CardDescription, CardHeader, CardTitle } from '../Card';

export function WelcomeWidget() {
  return (
    <Card className="bg-gradient-to-r from-primary/20 to-purple-900/20 border-primary/20">
      <CardHeader>
        <CardTitle className="text-2xl">Welcome to FocusDeck</CardTitle>
        <CardDescription>
          Your productivity companion for lectures, focus sessions, notes, and design work.
        </CardDescription>
      </CardHeader>
    </Card>
  );
}
