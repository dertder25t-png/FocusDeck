import { useState, useEffect } from 'react'
import { Button } from '../components/Button'
import { Input } from '../components/Input'
import { Badge } from '../components/Badge'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/Card'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '../components/Dialog'
import { apiFetch } from '../lib/utils'

interface TenantMember {
  id: string;
  userId: string;
  userName: string;
  userEmail: string;
  role: 'Owner' | 'Admin' | 'Member';
  joinedAt: string;
}

interface Invite {
  id: string;
  email: string;
  role: 'Owner' | 'Admin' | 'Member';
  createdAt: string;
  expiresAt: string;
  isExpired: boolean;
  isAccepted: boolean;
}

export function TenantSettingsPage() {
  const [tenantId] = useState('tenant-1') // TODO: wire to router context
  const [members, setMembers] = useState<TenantMember[]>([]);
  const [invites, setInvites] = useState<Invite[]>([]);
  const [showInviteDialog, setShowInviteDialog] = useState(false);
  const [inviteEmail, setInviteEmail] = useState('');
  const [inviteRole, setInviteRole] = useState<'Admin' | 'Member'>('Member');
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    loadMembers();
    loadInvites();
  }, [tenantId]);

  const loadMembers = async () => {
    try {
      const res = await apiFetch(`/v1/tenants/${tenantId}/members`)
      if (res.ok) {
        const data = await res.json();
        setMembers(data);
      }
    } catch (err) {
      console.error('Failed to load members:', err);
    }
  };

  const loadInvites = async () => {
    try {
      const res = await apiFetch(`/v1/tenant-invites?tenantId=${tenantId}`)
      if (res.ok) {
        const data = await res.json();
        setInvites(data);
      }
    } catch (err) {
      console.error('Failed to load invites:', err);
    }
  };

  const handleInvite = async () => {
    if (!inviteEmail || !inviteEmail.includes('@')) return;
    
    setLoading(true);
    try {
      const res = await apiFetch(`/v1/tenant-invites?tenantId=${tenantId}`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({ email: inviteEmail, role: inviteRole })
      });
      
      if (res.ok) {
        setShowInviteDialog(false);
        setInviteEmail('');
        setInviteRole('Member');
        loadInvites();
      }
    } catch (err) {
      console.error('Failed to send invite:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleRemoveMember = async (memberId: string) => {
    if (!confirm('Remove this member from the tenant?')) return;

    try {
      const res = await apiFetch(`/v1/tenants/${tenantId}/members/${memberId}`, {
        method: 'DELETE'
      });
      
      if (res.ok) {
        loadMembers();
      }
    } catch (err) {
      console.error('Failed to remove member:', err);
    }
  };

  const handleRevokeInvite = async (inviteId: string) => {
    try {
      const res = await apiFetch(`/v1/tenant-invites/${inviteId}?tenantId=${tenantId}`, {
        method: 'DELETE'
      });
      
      if (res.ok) {
        loadInvites();
      }
    } catch (err) {
      console.error('Failed to revoke invite:', err);
    }
  };

  const getRoleBadge = (role: string) => {
    const variants: Record<string, 'success' | 'info' | 'default'> = {
      Owner: 'success',
      Admin: 'info',
      Member: 'default'
    };
    return <Badge variant={variants[role] || 'default'}>{role}</Badge>;
  };

  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-3xl font-semibold text-white">Tenant Settings</h1>
        <p className="text-gray-400 mt-2">Manage members, invites, and roles</p>
      </div>

      {/* Members Section */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle>Members</CardTitle>
              <CardDescription>Active tenant members</CardDescription>
            </div>
            <Button onClick={() => setShowInviteDialog(true)}>Invite Member</Button>
          </div>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {members.length === 0 ? (
              <p className="text-gray-400 text-center py-8">No members yet</p>
            ) : (
              members.map(member => (
                <div key={member.id} className="flex items-center justify-between p-4 border border-gray-700 rounded-lg">
                  <div>
                    <div className="flex items-center gap-3">
                      <span className="text-white font-medium">{member.userName}</span>
                      {getRoleBadge(member.role)}
                    </div>
                    <p className="text-gray-400 text-sm mt-1">{member.userEmail}</p>
                    <p className="text-gray-500 text-xs mt-1">
                      Joined {new Date(member.joinedAt).toLocaleDateString()}
                    </p>
                  </div>
                  {member.role !== 'Owner' && (
                    <Button
                      variant="danger"
                      size="sm"
                      onClick={() => handleRemoveMember(member.id)}
                    >
                      Remove
                    </Button>
                  )}
                </div>
              ))
            )}
          </div>
        </CardContent>
      </Card>

      {/* Pending Invites Section */}
      <Card>
        <CardHeader>
          <CardTitle>Pending Invites</CardTitle>
          <CardDescription>Invitations awaiting acceptance</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {invites.length === 0 ? (
              <p className="text-gray-400 text-center py-8">No pending invites</p>
            ) : (
              invites.map(invite => (
                <div key={invite.id} className="flex items-center justify-between p-4 border border-gray-700 rounded-lg">
                  <div>
                    <div className="flex items-center gap-3">
                      <span className="text-white font-medium">{invite.email}</span>
                      {getRoleBadge(invite.role)}
                    </div>
                    <p className="text-gray-500 text-xs mt-1">
                      Expires {new Date(invite.expiresAt).toLocaleDateString()}
                    </p>
                  </div>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => handleRevokeInvite(invite.id)}
                  >
                    Revoke
                  </Button>
                </div>
              ))
            )}
          </div>
        </CardContent>
      </Card>

      {/* Invite Dialog */}
      <Dialog open={showInviteDialog} onOpenChange={setShowInviteDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Invite Member</DialogTitle>
            <DialogDescription>
              Send an invitation to join your tenant
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div>
              <label className="text-sm text-gray-300 mb-2 block">Email</label>
              <Input
                type="email"
                placeholder="colleague@example.com"
                value={inviteEmail}
                onChange={(e) => setInviteEmail(e.target.value)}
              />
            </div>
            <div>
              <label className="text-sm text-gray-300 mb-2 block">Role</label>
              <select
                className="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-primary"
                value={inviteRole}
                onChange={(e) => setInviteRole(e.target.value as 'Admin' | 'Member')}
              >
                <option value="Member">Member</option>
                <option value="Admin">Admin</option>
              </select>
            </div>
          </div>
          <DialogFooter>
            <Button variant="ghost" onClick={() => setShowInviteDialog(false)}>
              Cancel
            </Button>
            <Button onClick={handleInvite} disabled={loading || !inviteEmail}>
              {loading ? 'Sending...' : 'Send Invite'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
