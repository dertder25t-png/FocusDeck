export type PrivacyTier = 'Low' | 'Medium' | 'High'

export interface PrivacySetting {
  contextType: string
  displayName: string
  description: string
  isEnabled: boolean
  tier: PrivacyTier
  defaultEnabled: boolean
}

export interface PrivacySettingUpdate {
  contextType: string
  isEnabled: boolean
}
