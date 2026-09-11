import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useAuth } from "@/lib/auth";
import { ApiError } from "@/lib/api";

export function Register() {
  const { register } = useAuth();
  const navigate = useNavigate();
  const [form, setForm] = useState({
    PersonName: "",
    Email: "",
    Phone: "",
    Password: "",
    ConfirmPassword: "",
  });
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  function set(key: keyof typeof form) {
    return (e: React.ChangeEvent<HTMLInputElement>) =>
      setForm((f) => ({ ...f, [key]: e.target.value }));
  }

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    if (form.Password !== form.ConfirmPassword) {
      setError("Passwords do not match.");
      return;
    }
    setBusy(true);
    try {
      await register({
        PersonName: form.PersonName.trim(),
        Email: form.Email.trim(),
        Phone: form.Phone.trim(),
        Password: form.Password,
        ConfirmPassword: form.ConfirmPassword,
      });
      navigate("/", { replace: true });
    } catch (err) {
      setError(err instanceof ApiError ? err.body || err.message : "Registration failed.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="mx-auto flex min-h-screen w-full max-w-sm flex-col justify-center px-6">
      <h1 className="text-2xl font-semibold tracking-tight">Create your account</h1>
      <p className="mt-1 text-sm text-muted-foreground">
        Your network stays private to you.
      </p>
      <form onSubmit={onSubmit} className="mt-8 flex flex-col gap-4">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="name">Name</Label>
          <Input id="name" required value={form.PersonName} onChange={set("PersonName")} />
        </div>
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="email">Email</Label>
          <Input id="email" type="email" required value={form.Email} onChange={set("Email")} />
        </div>
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="phone">Phone (digits only)</Label>
          <Input
            id="phone"
            required
            inputMode="numeric"
            pattern="[0-9]*"
            value={form.Phone}
            onChange={set("Phone")}
          />
        </div>
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="password">Password</Label>
          <Input
            id="password"
            type="password"
            autoComplete="new-password"
            required
            value={form.Password}
            onChange={set("Password")}
          />
        </div>
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="confirm">Confirm password</Label>
          <Input
            id="confirm"
            type="password"
            autoComplete="new-password"
            required
            value={form.ConfirmPassword}
            onChange={set("ConfirmPassword")}
          />
        </div>
        {error && <p className="text-sm text-destructive">{error}</p>}
        <Button type="submit" disabled={busy}>
          {busy ? "Creating…" : "Create account"}
        </Button>
      </form>
      <p className="mt-4 text-sm text-muted-foreground">
        Have an account?{" "}
        <Link to="/login" className="font-medium text-foreground underline">
          Sign in
        </Link>
      </p>
    </div>
  );
}
