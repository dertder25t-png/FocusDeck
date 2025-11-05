import { useState } from 'react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/Card'
import { Button } from '../components/Button'
import { Input } from '../components/Input'
import { Badge } from '../components/Badge'
import { EmptyState } from '../components/States'

interface Organization {
  id: string
  name: string
  slug: string
  memberCount: number
  userRole: string
}

export function OrganizationsPage() {
  const [orgs] = useState<Organization[]>([])
  const [showCreateDialog, setShowCreateDialog] = useState(false)
  const [name, setName] = useState('')
  const [slug, setSlug] = useState('')

  const handleCreateOrg = async () => {
    // TODO: API call to create organization
    console.log('Create org:', { name, slug })
    setShowCreateDialog(false)
    setName('')
    setSlug('')
  }

  const getRoleBadgeVariant = (role: string) => {
    switch (role.toLowerCase()) {
      case 'owner':
        return 'success'
      case 'admin':
        return 'info'
      default:
        return 'default'
    }
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold">Organizations</h1>
          <p className="text-sm text-gray-400 mt-1">
            Manage your organizations and switch between them
          </p>
        </div>
        <Button onClick={() => setShowCreateDialog(true)}>
          + New Organization
        </Button>
      </div>

      {showCreateDialog && (
        <Card>
          <CardHeader>
            <CardTitle>Create New Organization</CardTitle>
            <CardDescription>
              Create a new organization to collaborate with your team
            </CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <label className="text-sm font-medium mb-2 block">Organization Name</label>
              <Input
                placeholder="Acme Inc."
                value={name}
                onChange={(e) => {
                  setName(e.target.value)
                  // Auto-generate slug from name
                  setSlug(e.target.value.toLowerCase().replace(/[^a-z0-9]/g, '-').replace(/--+/g, '-'))
                }}
              />
            </div>
            <div>
              <label className="text-sm font-medium mb-2 block">URL Slug</label>
              <Input
                placeholder="acme-inc"
                value={slug}
                onChange={(e) => setSlug(e.target.value.toLowerCase().replace(/[^a-z0-9-]/g, ''))}
              />
              <p className="text-xs text-gray-500 mt-1">
                Used in the URL: focusdeck.com/org/{slug || 'your-slug'}
              </p>
            </div>
            <div className="flex gap-3">
              <Button onClick={handleCreateOrg} disabled={!name || !slug}>
                Create Organization
              </Button>
              <Button variant="secondary" onClick={() => setShowCreateDialog(false)}>
                Cancel
              </Button>
            </div>
          </CardContent>
        </Card>
      )}

      {orgs.length === 0 ? (
        <Card>
          <CardContent className="py-12">
            <EmptyState
              icon="🏢"
              title="No organizations yet"
              description="Create your first organization to start collaborating with your team"
              action={{
                label: 'Create Organization',
                onClick: () => setShowCreateDialog(true),
              }}
            />
          </CardContent>
        </Card>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {orgs.map((org) => (
            <Card key={org.id} className="cursor-pointer hover:border-primary/50 transition-colors">
              <CardHeader>
                <div className="flex items-start justify-between">
                  <div className="flex-1">
                    <CardTitle>{org.name}</CardTitle>
                    <CardDescription className="mt-1">
                      @{org.slug}
                    </CardDescription>
                  </div>
                  <Badge variant={getRoleBadgeVariant(org.userRole) as any}>
                    {org.userRole}
                  </Badge>
                </div>
              </CardHeader>
              <CardContent>
                <div className="text-sm text-gray-400">
                  {org.memberCount} {org.memberCount === 1 ? 'member' : 'members'}
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  )
}
