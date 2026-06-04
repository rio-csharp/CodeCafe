function globMatches(value, pattern) {
  const regex = new RegExp(`^${pattern
    .split('**')
    .map(part => part
      .replace(/[.+^${}()|[\]\\]/g, '\\$&')
      .replace(/\*/g, '[^/]*'))
    .join('.*')}$`);
  return regex.test(value);
}

module.exports = async function resolveDeployCiRun({ github, context, core }) {
  const { owner, repo } = context.repo;
  const isDispatch = context.eventName === 'workflow_dispatch';
  const deployName = process.env.DEPLOY_NAME || 'deployment';
  const requiredWorkflowName = process.env.DEPLOY_REQUIRED_WORKFLOW_NAME || 'CI';
  const requiredEvent = process.env.DEPLOY_REQUIRED_EVENT || 'push';
  const allowedBranches = (process.env.DEPLOY_ALLOWED_BRANCHES || '')
    .split(/[\n,]/)
    .map(value => value.trim())
    .filter(Boolean);

  let run = context.payload.workflow_run;
  let ciRunId = run?.id;

  if (isDispatch) {
    ciRunId = Number(process.env.DEPLOY_CI_RUN_ID);
    if (!Number.isInteger(ciRunId) || ciRunId <= 0) {
      core.setFailed('ci_run_id must be a positive integer.');
      return;
    }

    const response = await github.rest.actions.getWorkflowRun({
      owner,
      repo,
      run_id: ciRunId,
    });
    run = response.data;
  }

  if (!run) {
    core.setOutput('skip', 'true');
    return;
  }

  const branch = run.head_branch || '';
  const workflowName = run.name || '';
  const branchAllowed = allowedBranches.length === 0 || allowedBranches.some(pattern => globMatches(branch, pattern));
  const workflowAllowed = !requiredWorkflowName || workflowName === requiredWorkflowName;
  const validRun = run.conclusion === 'success' && run.event === requiredEvent && branchAllowed && workflowAllowed;

  if (!validRun) {
    const message = `CI run ${ciRunId} is not valid for ${deployName}. workflow=${workflowName}, conclusion=${run.conclusion}, event=${run.event}, branch=${branch}.`;
    if (isDispatch) {
      core.setFailed(message);
    } else {
      core.info(message);
      core.setOutput('skip', 'true');
    }
    return;
  }

  core.setOutput('skip', 'false');
  core.setOutput('ci_run_id', String(ciRunId));
};
