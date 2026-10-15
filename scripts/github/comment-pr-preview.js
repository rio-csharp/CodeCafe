module.exports = async function commentPrPreview({ github, context, core }) {
  const marker = '<!-- codecafe-pr-preview -->';
  const prNumber = Number(process.env.PREVIEW_PR_NUMBER);

  if (!Number.isInteger(prNumber) || prNumber <= 0) {
    core.setFailed('PREVIEW_PR_NUMBER must be a positive integer.');
    return;
  }

  for (const name of ['FRONTEND_HOST', 'API_HOST', 'NAMESPACE', 'IMAGE_TAG']) {
    if (!process.env[name]) {
      core.setFailed(`${name} is required.`);
      return;
    }
  }

  const body = `${marker}
## PR Preview

| Service | URL |
| --- | --- |
| Frontend | \`https://${process.env.FRONTEND_HOST}\` |
| API readiness | \`https://${process.env.API_HOST}/health/ready\` |

Namespace: \`${process.env.NAMESPACE}\`
Image tag: \`${process.env.IMAGE_TAG}\`
`;

  const { owner, repo } = context.repo;
  const comments = await github.rest.issues.listComments({ owner, repo, issue_number: prNumber });
  const existing = comments.data.find(comment => comment.body?.includes(marker));

  if (existing) {
    await github.rest.issues.updateComment({ owner, repo, comment_id: existing.id, body });
  } else {
    await github.rest.issues.createComment({ owner, repo, issue_number: prNumber, body });
  }
};
