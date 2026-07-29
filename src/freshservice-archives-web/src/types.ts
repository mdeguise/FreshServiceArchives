export interface TicketListItem {
  ticketID: number
  subject: string | null
  status: number | null
  statusText: string | null
  priority: number | null
  priorityText: string | null
  groupID: number | null
  groupName: string | null
  requesterID: number | null
  requesterName: string | null
  requesterEmail: string | null
  responderID: number | null
  responderName: string | null
  createdAt: string | null
  updatedAt: string | null
  hasAttachments: boolean
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export interface PersonInfo {
  id: number
  firstName: string | null
  lastName: string | null
  email: string | null
  jobTitle: string | null
  active: boolean | null
  fullName: string
}

export interface GroupInfo {
  groupID: number
  name: string | null
  description: string | null
}

export interface ConversationItem {
  conversationID: number
  ticketID: number
  userID: number | null
  fromEmail: string | null
  toEmailsJson: string | null
  ccEmailsJson: string | null
  bodyHTML: string | null
  bodyText: string | null
  privateNote: boolean
  incoming: boolean
  conversationType: string | null
  createdAt: string | null
  updatedAt: string | null
}

export interface AttachmentItem {
  attachmentID: number
  freshserviceAttachmentID: number | null
  ticketID: number
  conversationID: number | null
  fileName: string | null
  contentType: string | null
  fileSize: number | null
  downloaded: boolean
  canDownload: boolean
}

export interface TicketDetail {
  ticketID: number
  subject: string | null
  descriptionHTML: string | null
  descriptionText: string | null
  status: number | null
  statusText: string | null
  priority: number | null
  priorityText: string | null
  source: number | null
  sourceText: string | null
  category: string | null
  subCategory: string | null
  itemCategory: string | null
  departmentID: number | null
  workspaceID: number | null
  createdAt: string | null
  updatedAt: string | null
  dueBy: string | null
  frDueBy: string | null
  customFieldsJson: string | null
  requester: PersonInfo | null
  responder: PersonInfo | null
  group: GroupInfo | null
  conversations: ConversationItem[]
  attachments: AttachmentItem[]
}

export interface GroupLookup {
  groupID: number
  name: string | null
}

export interface CodeLookup {
  value: number
  text: string
}

export interface CurrentUser {
  userName: string
  displayName: string
  isAuthenticated: boolean
}
