# Contributing to Hardwareless Procedural Music

## 🎯 Workflow

### Branch Strategy
- **main** - Production-ready code
- **development** - Integration branch
- **feature/*** - Feature development
- **bugfix/*** - Bug fixes
- **hotfix/*** - Emergency fixes

### Development Process

1. **Create Feature Branch**
   ```bash
   git checkout development
   git pull origin development
   git checkout -b feature/your-feature-name
   ```

2. **Make Changes**
   - Follow C# coding guidelines
   - Update documentation
   - Add tests where applicable

3. **Commit Changes**
   ```bash
   git add .
   git commit -m "feat: add new music synthesis feature"
   ```

4. **Push and Create PR**
   ```bash
   git push origin feature/your-feature-name
   ```
   Then create Pull Request to `development` branch

## 🎼 Music System Guidelines

### Audio Code Standards
- Use meaningful variable names
- Comment complex synthesis algorithms
- Follow Unity audio best practices
- Test performance impact

### Documentation Requirements
- Update `Assets/Documentation/ProceduralMusic.md`
- Add inline code comments
- Include usage examples
- Document new HUD features

## 🔍 Code Review Process

### Automated Checks
- CI/CD pipeline validation
- C# code analysis
- Documentation verification
- Git LFS tracking

### Manual Review
- Code quality assessment
- Performance impact evaluation
- Unity best practices compliance
- Music system integration testing

## 🎮 Testing Guidelines

### Music System Testing
1. **Open Unity Editor**
2. **Enter Play Mode**
3. **Press F9** - Debug HUD should appear
4. **Test new features** - Verify countdown, saves, etc.
5. **Check performance** - Monitor audio pipeline

### Integration Testing
- Test with different game states
- Verify save/load functionality
- Check audio quality and performance
- Validate HUD responsiveness

## 📝 Commit Message Format

Use conventional commits:
- `feat:` - New features
- `fix:` - Bug fixes
- `docs:` - Documentation updates
- `style:` - Code formatting
- `refactor:` - Code restructuring
- `test:` - Test additions
- `chore:` - Build/config changes

Examples:
```
feat: add auto-progression countdown to music HUD
fix: resolve audio clipping in procedural synthesis
docs: update music system API reference
```

## 🚀 Release Process

### Development → Main
1. Feature complete in development branch
2. Full testing cycle completed
3. Documentation updated
4. Performance validated
5. Create release PR to main

### Hotfix Process
1. Create hotfix branch from main
2. Apply minimal fix
3. Test thoroughly
4. Merge to main and development

## 🎵 Music-Specific Guidelines

### Adding New Audio Features
1. **Plan the integration** - How does it fit with existing system?
2. **Implement core functionality** - Focus on audio quality
3. **Add HUD integration** - Make it accessible to developers
4. **Document thoroughly** - Update all relevant docs
5. **Test extensively** - Verify no audio degradation

### Performance Considerations
- Monitor CPU usage during synthesis
- Test with multiple audio sources
- Validate memory usage patterns
- Check for audio dropouts

Ready to contribute to the future of procedural music in Unity! 🎼✨
